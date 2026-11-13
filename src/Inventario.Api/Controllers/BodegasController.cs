using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Controllers;

[ApiController]
[Route("api/bodegas")]
[Produces("application/json")]
public class BodegasController : ControllerBase
{
    private readonly InventarioDbContext _context;

    public BodegasController(InventarioDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BodegaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BodegaDto>>> ObtenerTodas(CancellationToken cancellationToken)
    {
        var bodegas = await _context.Bodegas
            .AsNoTracking()
            .Select(b => new BodegaDto
            {
                IdBodega = b.IdBodega,
                Nombre = b.Nombre,
                Ciudad = b.Ciudad,
                Direccion = b.Direccion,
                FechaCreacion = b.FechaCreacion
            })
            .ToListAsync(cancellationToken);

        return Ok(bodegas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BodegaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BodegaDto>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var bodega = await _context.Bodegas
            .AsNoTracking()
            .Where(b => b.IdBodega == id)
            .Select(b => new BodegaDto
            {
                IdBodega = b.IdBodega,
                Nombre = b.Nombre,
                Ciudad = b.Ciudad,
                Direccion = b.Direccion,
                FechaCreacion = b.FechaCreacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (bodega == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Bodega no encontrada",
                Detail = $"No existe una bodega con Id {id}."
            });
        }

        return Ok(bodega);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BodegaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BodegaDto>> Crear([FromBody] CrearBodegaDto dto, CancellationToken cancellationToken)
    {
        var existeNombre = await _context.Bodegas
            .AnyAsync(b => b.Nombre.ToLower() == dto.Nombre.Trim().ToLower() && b.Ciudad.ToLower() == dto.Ciudad.Trim().ToLower(), cancellationToken);

        if (existeNombre)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Bodega duplicada",
                Detail = $"Ya existe una bodega con el nombre '{dto.Nombre.Trim()}' en la ciudad de {dto.Ciudad.Trim()}."
            });
        }

        var nueva = new Bodega
        {
            Nombre = dto.Nombre.Trim(),
            Ciudad = dto.Ciudad.Trim(),
            Direccion = dto.Direccion.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.Bodegas.Add(nueva);
        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new BodegaDto
        {
            IdBodega = nueva.IdBodega,
            Nombre = nueva.Nombre,
            Ciudad = nueva.Ciudad,
            Direccion = nueva.Direccion,
            FechaCreacion = nueva.FechaCreacion
        };

        return CreatedAtAction(nameof(ObtenerPorId), new { id = nueva.IdBodega }, respuesta);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BodegaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BodegaDto>> Actualizar(int id, [FromBody] ActualizarBodegaDto dto, CancellationToken cancellationToken)
    {
        var bodega = await _context.Bodegas.FindAsync(new object[] { id }, cancellationToken);
        if (bodega == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Bodega no encontrada",
                Detail = $"No existe una bodega con Id {id}."
            });
        }

        var duplicado = await _context.Bodegas
            .AnyAsync(b => b.IdBodega != id && b.Nombre.ToLower() == dto.Nombre.Trim().ToLower() && b.Ciudad.ToLower() == dto.Ciudad.Trim().ToLower(), cancellationToken);

        if (duplicado)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Bodega duplicada",
                Detail = $"Ya existe otra bodega con el nombre '{dto.Nombre.Trim()}' en la ciudad de {dto.Ciudad.Trim()}."
            });
        }

        bodega.Nombre = dto.Nombre.Trim();
        bodega.Ciudad = dto.Ciudad.Trim();
        bodega.Direccion = dto.Direccion.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new BodegaDto
        {
            IdBodega = bodega.IdBodega,
            Nombre = bodega.Nombre,
            Ciudad = bodega.Ciudad,
            Direccion = bodega.Direccion,
            FechaCreacion = bodega.FechaCreacion
        };

        return Ok(respuesta);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var bodega = await _context.Bodegas.FindAsync(new object[] { id }, cancellationToken);
        if (bodega == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Bodega no encontrada",
                Detail = $"No existe una bodega con Id {id}."
            });
        }

        // Validación de integridad referencial física (Restrict)
        var tieneExistencias = await _context.Existencias.AnyAsync(e => e.IdBodega == id, cancellationToken);
        var tieneMovimientos = await _context.MovimientosInventario.AnyAsync(m => m.IdBodega == id, cancellationToken);
        var tieneReservas = await _context.ReservasStock.AnyAsync(r => r.IdBodega == id, cancellationToken);

        if (tieneExistencias || tieneMovimientos || tieneReservas)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Restricción de integridad referencial",
                Detail = $"No se puede eliminar la bodega '{bodega.Nombre}' (Id: {id}) porque tiene registros vinculados de existencias, movimientos o reservas. Regla física configurada: Restrict."
            });
        }

        _context.Bodegas.Remove(bodega);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
