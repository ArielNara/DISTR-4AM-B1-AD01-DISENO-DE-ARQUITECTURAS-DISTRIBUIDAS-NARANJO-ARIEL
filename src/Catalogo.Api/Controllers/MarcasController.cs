using Catalogo.Api.Data;
using Catalogo.Api.Dtos;
using Catalogo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Controllers;

[ApiController]
[Route("api/marcas")]
[Produces("application/json")]
public class MarcasController : ControllerBase
{
    private readonly CatalogoDbContext _context;

    public MarcasController(CatalogoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MarcaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MarcaDto>>> ObtenerTodas(CancellationToken cancellationToken)
    {
        var marcas = await _context.Marcas
            .AsNoTracking()
            .Select(m => new MarcaDto
            {
                IdMarca = m.IdMarca,
                Nombre = m.Nombre,
                PaisOrigen = m.PaisOrigen,
                FechaCreacion = m.FechaCreacion
            })
            .ToListAsync(cancellationToken);

        return Ok(marcas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MarcaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarcaDto>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var marca = await _context.Marcas
            .AsNoTracking()
            .Where(m => m.IdMarca == id)
            .Select(m => new MarcaDto
            {
                IdMarca = m.IdMarca,
                Nombre = m.Nombre,
                PaisOrigen = m.PaisOrigen,
                FechaCreacion = m.FechaCreacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (marca == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Marca no encontrada",
                Detail = $"No existe una marca con Id {id}."
            });
        }

        return Ok(marca);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MarcaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MarcaDto>> Crear([FromBody] CrearMarcaDto dto, CancellationToken cancellationToken)
    {
        var existeNombre = await _context.Marcas
            .AnyAsync(m => m.Nombre.ToLower() == dto.Nombre.Trim().ToLower(), cancellationToken);

        if (existeNombre)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Marca duplicada",
                Detail = $"Ya existe una marca registrada con el nombre '{dto.Nombre.Trim()}'."
            });
        }

        var nueva = new Marca
        {
            Nombre = dto.Nombre.Trim(),
            PaisOrigen = dto.PaisOrigen.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.Marcas.Add(nueva);
        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new MarcaDto
        {
            IdMarca = nueva.IdMarca,
            Nombre = nueva.Nombre,
            PaisOrigen = nueva.PaisOrigen,
            FechaCreacion = nueva.FechaCreacion
        };

        return CreatedAtAction(nameof(ObtenerPorId), new { id = nueva.IdMarca }, respuesta);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MarcaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MarcaDto>> Actualizar(int id, [FromBody] ActualizarMarcaDto dto, CancellationToken cancellationToken)
    {
        var marca = await _context.Marcas.FindAsync(new object[] { id }, cancellationToken);
        if (marca == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Marca no encontrada",
                Detail = $"No existe una marca con Id {id}."
            });
        }

        var duplicado = await _context.Marcas
            .AnyAsync(m => m.IdMarca != id && m.Nombre.ToLower() == dto.Nombre.Trim().ToLower(), cancellationToken);

        if (duplicado)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Nombre en uso",
                Detail = $"Ya existe otra marca con el nombre '{dto.Nombre.Trim()}'."
            });
        }

        marca.Nombre = dto.Nombre.Trim();
        marca.PaisOrigen = dto.PaisOrigen.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new MarcaDto
        {
            IdMarca = marca.IdMarca,
            Nombre = marca.Nombre,
            PaisOrigen = marca.PaisOrigen,
            FechaCreacion = marca.FechaCreacion
        };

        return Ok(respuesta);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var marca = await _context.Marcas.FindAsync(new object[] { id }, cancellationToken);
        if (marca == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Marca no encontrada",
                Detail = $"No existe una marca con Id {id}."
            });
        }

        // Verificación de integridad referencial física (Restrict)
        var tieneProductos = await _context.Productos.AnyAsync(p => p.IdMarca == id, cancellationToken);
        if (tieneProductos)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Restricción de integridad referencial",
                Detail = $"No se puede eliminar la marca '{marca.Nombre}' (Id: {id}) porque tiene productos asociados. Regla física configurada: Restrict."
            });
        }

        _context.Marcas.Remove(marca);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
