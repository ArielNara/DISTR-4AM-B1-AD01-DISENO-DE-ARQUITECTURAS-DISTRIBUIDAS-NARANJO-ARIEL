using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos.Api.Data;
using Pedidos.Api.Dtos;
using Pedidos.Api.Models;
using Pedidos.Api.Services;

namespace Pedidos.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
[Produces("application/json")]
public class PedidosController : ControllerBase
{
    private readonly PedidosDbContext _context;
    private readonly ICatalogoHttpClient _catalogoClient;
    private readonly IInventarioHttpClient _inventarioClient;

    public PedidosController(PedidosDbContext context, ICatalogoHttpClient catalogoClient, IInventarioHttpClient inventarioClient)
    {
        _context = context;
        _catalogoClient = catalogoClient;
        _inventarioClient = inventarioClient;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PedidoDto>>> ObtenerPedidos(
        [FromQuery] PedidoFiltrosDto filtros,
        CancellationToken cancellationToken)
    {
        var query = _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .AsQueryable();

        if (filtros.IdCliente.HasValue)
        {
            query = query.Where(p => p.IdCliente == filtros.IdCliente.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtros.Estado))
        {
            query = query.Where(p => p.Estado == filtros.Estado.Trim().ToUpper());
        }

        if (filtros.FechaDesde.HasValue)
        {
            query = query.Where(p => p.Fecha >= filtros.FechaDesde.Value);
        }

        if (filtros.FechaHasta.HasValue)
        {
            query = query.Where(p => p.Fecha <= filtros.FechaHasta.Value);
        }

        var pedidos = await query
            .OrderByDescending(p => p.Fecha)
            .Select(p => new PedidoDto
            {
                IdPedido = p.IdPedido,
                IdCliente = p.IdCliente,
                ClienteNombre = p.Cliente != null ? p.Cliente.Nombres : string.Empty,
                Fecha = p.Fecha,
                Total = p.Total,
                Estado = p.Estado
            })
            .ToListAsync(cancellationToken);

        return Ok(pedidos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDetalleDto>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var pedido = await _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Detalles)
            .Where(p => p.IdPedido == id)
            .Select(p => new PedidoDetalleDto
            {
                IdPedido = p.IdPedido,
                IdCliente = p.IdCliente,
                ClienteNombre = p.Cliente != null ? p.Cliente.Nombres : string.Empty,
                Fecha = p.Fecha,
                Total = p.Total,
                Estado = p.Estado,
                Detalles = p.Detalles.Select(d => new DetallePedidoDto
                {
                    IdDetalle = d.IdDetalle,
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    IdReservaInventario = d.IdReservaInventario
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (pedido == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Pedido no encontrado",
                Detail = $"No existe un pedido con Id {id}."
            });
        }

        return Ok(pedido);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PedidoDetalleDto>> CrearPedido([FromBody] CrearPedidoDto dto, CancellationToken cancellationToken)
    {
        var clienteExiste = await _context.Clientes.AnyAsync(c => c.IdCliente == dto.IdCliente, cancellationToken);
        if (!clienteExiste)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Cliente no encontrado",
                Detail = $"No existe un cliente con Id {dto.IdCliente}."
            });
        }

        if (dto.Detalles == null || dto.Detalles.Count == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(dto.Detalles)] = new[] { "Debe incluir al menos un detalle en el pedido." }
            }));
        }

        var reservadas = new List<int>();
        var detalles = new List<DetallePedido>();
        decimal totalPedido = 0m;

        try
        {
            foreach (var item in dto.Detalles)
            {
                var producto = await _catalogoClient.ObtenerProductoPorIdAsync(item.IdProducto, cancellationToken);
                if (producto == null)
                {
                    await CompensarReservasAsync(reservadas, cancellationToken);
                    return NotFound(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Producto no encontrado en Catálogo",
                        Detail = $"El producto con Id {item.IdProducto} no existe en Catalogo.Api."
                    });
                }

                if (producto.Estado != "ACTIVO")
                {
                    await CompensarReservasAsync(reservadas, cancellationToken);
                    return BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Producto inactivo",
                        Detail = $"El producto '{producto.Nombre}' (Id: {item.IdProducto}) se encuentra inactivo en el catálogo."
                    });
                }

                var reserva = await _inventarioClient.ReservarStockAsync(new CrearReservaInventarioDto
                {
                    IdProducto = item.IdProducto,
                    IdBodega = item.IdBodega,
                    Cantidad = item.Cantidad,
                    ReferenciaExterna = $"PEDIDO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"
                }, cancellationToken);

                reservadas.Add(reserva.IdReserva);

                var precioUnitario = producto.Precio;
                totalPedido += precioUnitario * item.Cantidad;

                detalles.Add(new DetallePedido
                {
                    IdProducto = item.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precioUnitario,
                    IdReservaInventario = reserva.IdReserva
                });
            }

            var pedido = new Pedido
            {
                IdCliente = dto.IdCliente,
                Fecha = DateTime.UtcNow,
                Total = totalPedido,
                Estado = "PENDIENTE",
                Detalles = detalles
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var detalle in pedido.Detalles)
            {
                if (detalle.IdReservaInventario.HasValue)
                {
                    detalle.IdPedido = pedido.IdPedido;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var reservaId in reservadas)
            {
                await _inventarioClient.ConfirmarReservaAsync(reservaId, cancellationToken);
            }

            pedido.Estado = "CONFIRMADO";
            await _context.SaveChangesAsync(cancellationToken);

            var respuesta = new PedidoDetalleDto
            {
                IdPedido = pedido.IdPedido,
                IdCliente = pedido.IdCliente,
                ClienteNombre = await _context.Clientes.Where(c => c.IdCliente == pedido.IdCliente).Select(c => c.Nombres).FirstOrDefaultAsync(cancellationToken) ?? string.Empty,
                Fecha = pedido.Fecha,
                Total = pedido.Total,
                Estado = pedido.Estado,
                Detalles = pedido.Detalles.Select(d => new DetallePedidoDto
                {
                    IdDetalle = d.IdDetalle,
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    IdReservaInventario = d.IdReservaInventario
                }).ToList()
            };

            return CreatedAtAction(nameof(ObtenerPorId), new { id = pedido.IdPedido }, respuesta);
        }
        catch (ServicioNoDisponibleException)
        {
            await CompensarReservasAsync(reservadas, cancellationToken);
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No hay suficiente stock disponible") || ex.Message.Contains("Stock disponible insuficiente") || ex.Message.Contains("No se pudo") || ex.Message.Contains("no pudo"))
        {
            await CompensarReservasAsync(reservadas, cancellationToken);
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No se pudo reservar el stock",
                Detail = ex.Message
            });
        }
        catch (Exception)
        {
            await CompensarReservasAsync(reservadas, cancellationToken);
            throw;
        }
    }

    [HttpPut("{id:int}/cancelar")]
    [ProducesResponseType(typeof(PedidoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PedidoDetalleDto>> CancelarPedido(int id, CancellationToken cancellationToken)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.IdPedido == id, cancellationToken);

        if (pedido == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Pedido no encontrado",
                Detail = $"No existe un pedido con Id {id}."
            });
        }

        if (pedido.Estado == "CANCELADO")
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Pedido ya cancelado",
                Detail = $"El pedido {id} ya se encuentra cancelado."
            });
        }

        var reservas = pedido.Detalles
            .Where(d => d.IdReservaInventario.HasValue)
            .Select(d => d.IdReservaInventario!.Value)
            .Distinct()
            .ToList();

        foreach (var reservaId in reservas)
        {
            await _inventarioClient.CancelarReservaAsync(reservaId, cancellationToken);
        }

        pedido.Estado = "CANCELADO";
        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new PedidoDetalleDto
        {
            IdPedido = pedido.IdPedido,
            IdCliente = pedido.IdCliente,
            ClienteNombre = pedido.Cliente?.Nombres ?? string.Empty,
            Fecha = pedido.Fecha,
            Total = pedido.Total,
            Estado = pedido.Estado,
            Detalles = pedido.Detalles.Select(d => new DetallePedidoDto
            {
                IdDetalle = d.IdDetalle,
                IdProducto = d.IdProducto,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                IdReservaInventario = d.IdReservaInventario
            }).ToList()
        };

        return Ok(respuesta);
    }

    private async Task CompensarReservasAsync(IEnumerable<int> reservas, CancellationToken cancellationToken)
    {
        foreach (var reservaId in reservas)
        {
            try
            {
                await _inventarioClient.CancelarReservaAsync(reservaId, cancellationToken);
            }
            catch
            {
                // La compensación no debe romper la respuesta principal si el servicio de inventario ya está caído.
            }
        }
    }
}
