using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Controllers;

[ApiController]
[Route("api/reservas")]
[Produces("application/json")]
public class ReservasController : ControllerBase
{
    private readonly InventarioDbContext _context;
    private readonly ICatalogoHttpClient _catalogoClient;

    public ReservasController(InventarioDbContext context, ICatalogoHttpClient catalogoClient)
    {
        _context = context;
        _catalogoClient = catalogoClient;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReservaStockDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReservaStockDto>>> ObtenerReservas(
        [FromQuery] ReservaFiltrosDto filtros,
        CancellationToken cancellationToken)
    {
        var query = _context.ReservasStock
            .AsNoTracking()
            .Include(r => r.Bodega)
            .AsQueryable();

        if (filtros.IdProducto.HasValue)
        {
            query = query.Where(r => r.IdProducto == filtros.IdProducto.Value);
        }

        if (filtros.IdBodega.HasValue)
        {
            query = query.Where(r => r.IdBodega == filtros.IdBodega.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtros.Estado))
        {
            query = query.Where(r => r.Estado == filtros.Estado.Trim().ToUpper());
        }

        var reservas = await query
            .OrderByDescending(r => r.FechaCreacion)
            .Select(r => new ReservaStockDto
            {
                IdReserva = r.IdReserva,
                IdProducto = r.IdProducto,
                IdBodega = r.IdBodega,
                BodegaNombre = r.Bodega != null ? r.Bodega.Nombre : null,
                Cantidad = r.Cantidad,
                Estado = r.Estado,
                ReferenciaExterna = r.ReferenciaExterna,
                FechaCreacion = r.FechaCreacion,
                FechaModificacion = r.FechaModificacion
            })
            .ToListAsync(cancellationToken);

        return Ok(reservas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReservaStockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservaStockDto>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var reserva = await _context.ReservasStock
            .AsNoTracking()
            .Include(r => r.Bodega)
            .Where(r => r.IdReserva == id)
            .Select(r => new ReservaStockDto
            {
                IdReserva = r.IdReserva,
                IdProducto = r.IdProducto,
                IdBodega = r.IdBodega,
                BodegaNombre = r.Bodega != null ? r.Bodega.Nombre : null,
                Cantidad = r.Cantidad,
                Estado = r.Estado,
                ReferenciaExterna = r.ReferenciaExterna,
                FechaCreacion = r.FechaCreacion,
                FechaModificacion = r.FechaModificacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (reserva == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Reserva no encontrada",
                Detail = $"No existe una reserva con Id {id}."
            });
        }

        return Ok(reserva);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservaStockDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaStockDto>> CrearReserva(
        [FromBody] CrearReservaStockDto dto,
        CancellationToken cancellationToken)
    {
        // 1. Validar producto contra Catalogo.Api (Referencia Lógica)
        var producto = await _catalogoClient.ObtenerProductoPorIdAsync(dto.IdProducto, cancellationToken);
        if (producto == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Producto no encontrado en Catálogo",
                Detail = $"No es posible reservar stock porque el producto con Id {dto.IdProducto} no existe en Catalogo.Api."
            });
        }

        if (producto.Estado != "ACTIVO")
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Producto inactivo",
                Detail = $"El producto '{producto.Nombre}' (Id: {dto.IdProducto}) se encuentra inactivo en el catálogo comercial."
            });
        }

        // 2. Validar bodega existente (FK física)
        var bodega = await _context.Bodegas.FindAsync(new object[] { dto.IdBodega }, cancellationToken);
        if (bodega == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Bodega no encontrada",
                Detail = $"No existe una bodega con Id {dto.IdBodega}."
            });
        }

        // 3. Validar existencia física del producto en la bodega
        var existencia = await _context.Existencias
            .FirstOrDefaultAsync(e => e.IdProducto == dto.IdProducto && e.IdBodega == dto.IdBodega, cancellationToken);

        var stockFisico = existencia?.Stock ?? 0;

        // 4. Calcular reservas activas actuales
        var reservasActivas = await _context.ReservasStock
            .Where(r => r.IdProducto == dto.IdProducto && r.IdBodega == dto.IdBodega && r.Estado == "ACTIVA")
            .SumAsync(r => r.Cantidad, cancellationToken);

        var stockDisponible = stockFisico - reservasActivas;

        if (stockDisponible < dto.Cantidad)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Stock disponible insuficiente",
                Detail = $"No hay suficiente stock disponible para reservar en {bodega.Nombre}. Stock físico: {stockFisico}, Reservas activas: {reservasActivas}, Disponible: {stockDisponible}, Solicitado: {dto.Cantidad}."
            });
        }

        // 5. Crear la reserva en estado ACTIVA
        var nuevaReserva = new ReservaStock
        {
            IdProducto = dto.IdProducto,
            IdBodega = dto.IdBodega,
            Cantidad = dto.Cantidad,
            Estado = "ACTIVA",
            ReferenciaExterna = dto.ReferenciaExterna?.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.ReservasStock.Add(nuevaReserva);
        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new ReservaStockDto
        {
            IdReserva = nuevaReserva.IdReserva,
            IdProducto = nuevaReserva.IdProducto,
            IdBodega = nuevaReserva.IdBodega,
            BodegaNombre = bodega.Nombre,
            Cantidad = nuevaReserva.Cantidad,
            Estado = nuevaReserva.Estado,
            ReferenciaExterna = nuevaReserva.ReferenciaExterna,
            FechaCreacion = nuevaReserva.FechaCreacion
        };

        return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevaReserva.IdReserva }, respuesta);
    }

    [HttpPut("{id:int}/confirmar")]
    [ProducesResponseType(typeof(ReservaStockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaStockDto>> ConfirmarReserva(int id, CancellationToken cancellationToken)
    {
        var reserva = await _context.ReservasStock
            .Include(r => r.Bodega)
            .FirstOrDefaultAsync(r => r.IdReserva == id, cancellationToken);

        if (reserva == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Reserva no encontrada",
                Detail = $"No existe una reserva con Id {id}."
            });
        }

        if (reserva.Estado != "ACTIVA")
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Estado de reserva inválido",
                Detail = $"Solo se pueden confirmar reservas en estado ACTIVA. La reserva {id} se encuentra en estado '{reserva.Estado}'."
            });
        }

        using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var existencia = await _context.Existencias
            .FirstOrDefaultAsync(e => e.IdProducto == reserva.IdProducto && e.IdBodega == reserva.IdBodega, cancellationToken);

        if (existencia == null || existencia.Stock < reserva.Cantidad)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Stock físico insuficiente",
                Detail = $"No hay suficiente stock físico para confirmar la reserva #{id}."
            });
        }

        // 1. Descontar stock físico
        existencia.Stock -= reserva.Cantidad;
        existencia.UltimaActualizacion = DateTime.UtcNow;

        // 2. Registrar movimiento de salida
        var movimiento = new MovimientoInventario
        {
            IdProducto = reserva.IdProducto,
            IdBodega = reserva.IdBodega,
            Tipo = "SALIDA",
            Cantidad = reserva.Cantidad,
            Fecha = DateTime.UtcNow,
            Observaciones = $"Confirmación automática de reserva #{reserva.IdReserva} (Ref: {reserva.ReferenciaExterna ?? "N/A"})"
        };
        _context.MovimientosInventario.Add(movimiento);

        // 3. Cambiar estado a CONFIRMADA
        reserva.Estado = "CONFIRMADA";
        reserva.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var respuesta = new ReservaStockDto
        {
            IdReserva = reserva.IdReserva,
            IdProducto = reserva.IdProducto,
            IdBodega = reserva.IdBodega,
            BodegaNombre = reserva.Bodega?.Nombre,
            Cantidad = reserva.Cantidad,
            Estado = reserva.Estado,
            ReferenciaExterna = reserva.ReferenciaExterna,
            FechaCreacion = reserva.FechaCreacion,
            FechaModificacion = reserva.FechaModificacion
        };

        return Ok(respuesta);
    }

    [HttpPut("{id:int}/cancelar")]
    [ProducesResponseType(typeof(ReservaStockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaStockDto>> CancelarReserva(int id, CancellationToken cancellationToken)
    {
        var reserva = await _context.ReservasStock
            .Include(r => r.Bodega)
            .FirstOrDefaultAsync(r => r.IdReserva == id, cancellationToken);

        if (reserva == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Reserva no encontrada",
                Detail = $"No existe una reserva con Id {id}."
            });
        }

        if (reserva.Estado != "ACTIVA")
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Estado de reserva inválido",
                Detail = $"Solo se pueden cancelar reservas en estado ACTIVA. La reserva {id} se encuentra en estado '{reserva.Estado}'."
            });
        }

        reserva.Estado = "CANCELADA";
        reserva.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new ReservaStockDto
        {
            IdReserva = reserva.IdReserva,
            IdProducto = reserva.IdProducto,
            IdBodega = reserva.IdBodega,
            BodegaNombre = reserva.Bodega?.Nombre,
            Cantidad = reserva.Cantidad,
            Estado = reserva.Estado,
            ReferenciaExterna = reserva.ReferenciaExterna,
            FechaCreacion = reserva.FechaCreacion,
            FechaModificacion = reserva.FechaModificacion
        };

        return Ok(respuesta);
    }
}
