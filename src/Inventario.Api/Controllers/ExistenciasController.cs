using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Controllers;

[ApiController]
[Route("api/existencias")]
[Produces("application/json")]
public class ExistenciasController : ControllerBase
{
    private readonly InventarioDbContext _context;
    private readonly ICatalogoHttpClient _catalogoClient;

    public ExistenciasController(InventarioDbContext context, ICatalogoHttpClient catalogoClient)
    {
        _context = context;
        _catalogoClient = catalogoClient;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExistenciaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExistenciaDto>>> ObtenerExistencias(
        [FromQuery] ExistenciaFiltrosDto filtros,
        CancellationToken cancellationToken)
    {
        var query = _context.Existencias
            .AsNoTracking()
            .Include(e => e.Bodega)
            .AsQueryable();

        if (filtros.IdProducto.HasValue)
        {
            query = query.Where(e => e.IdProducto == filtros.IdProducto.Value);
        }

        if (filtros.IdBodega.HasValue)
        {
            query = query.Where(e => e.IdBodega == filtros.IdBodega.Value);
        }

        var existencias = await query.ToListAsync(cancellationToken);

        // Obtenemos las reservas activas para calcular el stock disponible
        var reservasActivas = await _context.ReservasStock
            .AsNoTracking()
            .Where(r => r.Estado == "ACTIVA")
            .GroupBy(r => new { r.IdProducto, r.IdBodega })
            .Select(g => new { g.Key.IdProducto, g.Key.IdBodega, TotalReservado = g.Sum(r => r.Cantidad) })
            .ToListAsync(cancellationToken);

        var resultado = existencias.Select(e =>
        {
            var res = reservasActivas.FirstOrDefault(r => r.IdProducto == e.IdProducto && r.IdBodega == e.IdBodega);
            var cantReservada = res?.TotalReservado ?? 0;

            return new ExistenciaDto
            {
                IdExistencia = e.IdExistencia,
                IdProducto = e.IdProducto,
                IdBodega = e.IdBodega,
                BodegaNombre = e.Bodega?.Nombre,
                BodegaCiudad = e.Bodega?.Ciudad,
                Stock = e.Stock,
                ReservasActivas = cantReservada,
                UltimaActualizacion = e.UltimaActualizacion
            };
        }).ToList();

        return Ok(resultado);
    }

    [HttpGet("producto/{idProducto:int}")]
    [ProducesResponseType(typeof(ExistenciaResumenProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExistenciaResumenProductoDto>> ObtenerPorProducto(int idProducto, CancellationToken cancellationToken)
    {
        // Validación lógica inter-servicio: Verificar existencia y estado del producto en Catalogo.Api
        var producto = await _catalogoClient.ObtenerProductoPorIdAsync(idProducto, cancellationToken);
        if (producto == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Producto no encontrado en Catálogo",
                Detail = $"El producto con Id {idProducto} no existe en el catálogo comercial."
            });
        }

        var existencias = await _context.Existencias
            .AsNoTracking()
            .Include(e => e.Bodega)
            .Where(e => e.IdProducto == idProducto)
            .ToListAsync(cancellationToken);

        var reservasActivas = await _context.ReservasStock
            .AsNoTracking()
            .Where(r => r.IdProducto == idProducto && r.Estado == "ACTIVA")
            .GroupBy(r => r.IdBodega)
            .Select(g => new { IdBodega = g.Key, TotalReservado = g.Sum(r => r.Cantidad) })
            .ToListAsync(cancellationToken);

        var bodegas = existencias.Select(e =>
        {
            var res = reservasActivas.FirstOrDefault(r => r.IdBodega == e.IdBodega);
            var reservado = res?.TotalReservado ?? 0;

            return new BodegaStockDto
            {
                IdBodega = e.IdBodega,
                BodegaNombre = e.Bodega?.Nombre ?? string.Empty,
                Ciudad = e.Bodega?.Ciudad ?? string.Empty,
                StockFisico = e.Stock,
                ReservasActivas = reservado
            };
        }).ToList();

        var resumen = new ExistenciaResumenProductoDto
        {
            IdProducto = idProducto,
            TotalStockFisico = bodegas.Sum(b => b.StockFisico),
            TotalReservasActivas = bodegas.Sum(b => b.ReservasActivas),
            Bodegas = bodegas
        };

        return Ok(resumen);
    }

    [HttpPut("ajuste")]
    [ProducesResponseType(typeof(ExistenciaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExistenciaDto>> AjustarStock([FromBody] AjustarExistenciaDto dto, CancellationToken cancellationToken)
    {
        // 1. Validar producto contra Catalogo.Api (Referencia Lógica)
        var producto = await _catalogoClient.ObtenerProductoPorIdAsync(dto.IdProducto, cancellationToken);
        if (producto == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Producto no existe en Catálogo",
                Detail = $"No es posible ajustar inventario porque el producto con Id {dto.IdProducto} no existe en Catalogo.Api."
            });
        }

        if (producto.Estado != "ACTIVO")
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Producto inactivo",
                Detail = $"El producto '{producto.Nombre}' (Id: {dto.IdProducto}) se encuentra INACTIVO en el catálogo."
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

        // 3. Ejecutar ajuste dentro de transacción
        using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var existencia = await _context.Existencias
            .FirstOrDefaultAsync(e => e.IdProducto == dto.IdProducto && e.IdBodega == dto.IdBodega, cancellationToken);

        if (existencia == null)
        {
            existencia = new Existencia
            {
                IdProducto = dto.IdProducto,
                IdBodega = dto.IdBodega,
                Stock = dto.NuevoStock,
                UltimaActualizacion = DateTime.UtcNow
            };
            _context.Existencias.Add(existencia);
        }
        else
        {
            existencia.Stock = dto.NuevoStock;
            existencia.UltimaActualizacion = DateTime.UtcNow;
        }

        var movimiento = new MovimientoInventario
        {
            IdProducto = dto.IdProducto,
            IdBodega = dto.IdBodega,
            Tipo = "AJUSTE",
            Cantidad = dto.NuevoStock,
            Fecha = DateTime.UtcNow,
            Observaciones = dto.Motivo ?? "Ajuste directo de existencias"
        };
        _context.MovimientosInventario.Add(movimiento);

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var reservasActivas = await _context.ReservasStock
            .Where(r => r.IdProducto == dto.IdProducto && r.IdBodega == dto.IdBodega && r.Estado == "ACTIVA")
            .SumAsync(r => r.Cantidad, cancellationToken);

        var respuesta = new ExistenciaDto
        {
            IdExistencia = existencia.IdExistencia,
            IdProducto = existencia.IdProducto,
            IdBodega = existencia.IdBodega,
            BodegaNombre = bodega.Nombre,
            BodegaCiudad = bodega.Ciudad,
            Stock = existencia.Stock,
            ReservasActivas = reservasActivas,
            UltimaActualizacion = existencia.UltimaActualizacion
        };

        return Ok(respuesta);
    }
}
