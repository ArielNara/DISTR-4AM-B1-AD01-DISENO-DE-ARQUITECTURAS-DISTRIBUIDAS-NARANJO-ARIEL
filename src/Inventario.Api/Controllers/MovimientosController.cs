using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Controllers;

[ApiController]
[Route("api/movimientos")]
[Produces("application/json")]
public class MovimientosController : ControllerBase
{
    private readonly InventarioDbContext _context;
    private readonly ICatalogoHttpClient _catalogoClient;

    public MovimientosController(InventarioDbContext context, ICatalogoHttpClient catalogoClient)
    {
        _context = context;
        _catalogoClient = catalogoClient;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MovimientoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MovimientoDto>>> ObtenerMovimientos(
        [FromQuery] MovimientoFiltrosDto filtros,
        CancellationToken cancellationToken)
    {
        var query = _context.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Bodega)
            .AsQueryable();

        if (filtros.IdProducto.HasValue)
        {
            query = query.Where(m => m.IdProducto == filtros.IdProducto.Value);
        }

        if (filtros.IdBodega.HasValue)
        {
            query = query.Where(m => m.IdBodega == filtros.IdBodega.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtros.Tipo))
        {
            query = query.Where(m => m.Tipo == filtros.Tipo.Trim().ToUpper());
        }

        if (filtros.FechaInicio.HasValue)
        {
            query = query.Where(m => m.Fecha >= filtros.FechaInicio.Value);
        }

        if (filtros.FechaFin.HasValue)
        {
            query = query.Where(m => m.Fecha <= filtros.FechaFin.Value);
        }

        var movimientos = await query
            .OrderByDescending(m => m.Fecha)
            .Select(m => new MovimientoDto
            {
                IdMovimiento = m.IdMovimiento,
                IdProducto = m.IdProducto,
                IdBodega = m.IdBodega,
                BodegaNombre = m.Bodega != null ? m.Bodega.Nombre : null,
                Tipo = m.Tipo,
                Cantidad = m.Cantidad,
                Fecha = m.Fecha,
                Observaciones = m.Observaciones
            })
            .ToListAsync(cancellationToken);

        return Ok(movimientos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MovimientoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovimientoDto>> RegistrarMovimiento(
        [FromBody] CrearMovimientoDto dto,
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
                Detail = $"No es posible registrar el movimiento porque el producto con Id {dto.IdProducto} no existe en Catalogo.Api."
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

        var tipo = dto.Tipo.Trim().ToUpper();

        // 3. Ejecutar actualización de Existencia y Movimiento en transacción
        using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var existencia = await _context.Existencias
            .FirstOrDefaultAsync(e => e.IdProducto == dto.IdProducto && e.IdBodega == dto.IdBodega, cancellationToken);

        if (existencia == null)
        {
            if (tipo == "SALIDA")
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Stock insuficiente",
                    Detail = $"No existen existencias registradas del producto {dto.IdProducto} en la bodega {bodega.Nombre} para realizar una SALIDA."
                });
            }

            existencia = new Existencia
            {
                IdProducto = dto.IdProducto,
                IdBodega = dto.IdBodega,
                Stock = 0,
                UltimaActualizacion = DateTime.UtcNow
            };
            _context.Existencias.Add(existencia);
        }

        switch (tipo)
        {
            case "ENTRADA":
                existencia.Stock += dto.Cantidad;
                break;

            case "SALIDA":
                if (existencia.Stock < dto.Cantidad)
                {
                    return Conflict(new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Stock insuficiente",
                        Detail = $"Stock insuficiente en {bodega.Nombre}. Stock actual: {existencia.Stock}, cantidad a retirar: {dto.Cantidad}."
                    });
                }
                existencia.Stock -= dto.Cantidad;
                break;

            case "AJUSTE":
                existencia.Stock = dto.Cantidad;
                break;
        }

        existencia.UltimaActualizacion = DateTime.UtcNow;

        var movimiento = new MovimientoInventario
        {
            IdProducto = dto.IdProducto,
            IdBodega = dto.IdBodega,
            Tipo = tipo,
            Cantidad = dto.Cantidad,
            Fecha = DateTime.UtcNow,
            Observaciones = dto.Observaciones?.Trim()
        };

        _context.MovimientosInventario.Add(movimiento);
        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var respuesta = new MovimientoDto
        {
            IdMovimiento = movimiento.IdMovimiento,
            IdProducto = movimiento.IdProducto,
            IdBodega = movimiento.IdBodega,
            BodegaNombre = bodega.Nombre,
            Tipo = movimiento.Tipo,
            Cantidad = movimiento.Cantidad,
            Fecha = movimiento.Fecha,
            Observaciones = movimiento.Observaciones
        };

        return CreatedAtAction(nameof(ObtenerMovimientos), new { idProducto = movimiento.IdProducto }, respuesta);
    }
}
