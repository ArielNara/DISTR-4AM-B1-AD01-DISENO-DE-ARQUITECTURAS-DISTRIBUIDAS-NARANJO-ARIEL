using Catalogo.Api.Data;
using Catalogo.Api.Dtos;
using Catalogo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Controllers;

[ApiController]
[Route("api/categorias")]
[Produces("application/json")]
public class CategoriasController : ControllerBase
{
    private readonly CatalogoDbContext _context;

    public CategoriasController(CatalogoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoriaDto>>> ObtenerTodas(CancellationToken cancellationToken)
    {
        var categorias = await _context.Categorias
            .AsNoTracking()
            .Select(c => new CategoriaDto
            {
                IdCategoria = c.IdCategoria,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                FechaCreacion = c.FechaCreacion
            })
            .ToListAsync(cancellationToken);

        return Ok(categorias);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaDto>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var categoria = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.IdCategoria == id)
            .Select(c => new CategoriaDto
            {
                IdCategoria = c.IdCategoria,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                FechaCreacion = c.FechaCreacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (categoria == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Categoría no encontrada",
                Detail = $"No existe una categoría con Id {id}."
            });
        }

        return Ok(categoria);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaDto>> Crear([FromBody] CrearCategoriaDto dto, CancellationToken cancellationToken)
    {
        var existeNombre = await _context.Categorias
            .AnyAsync(c => c.Nombre.ToLower() == dto.Nombre.Trim().ToLower(), cancellationToken);

        if (existeNombre)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Categoría duplicada",
                Detail = $"Ya existe una categoría registrada con el nombre '{dto.Nombre.Trim()}'."
            });
        }

        var nueva = new Categoria
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.Categorias.Add(nueva);
        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new CategoriaDto
        {
            IdCategoria = nueva.IdCategoria,
            Nombre = nueva.Nombre,
            Descripcion = nueva.Descripcion,
            FechaCreacion = nueva.FechaCreacion
        };

        return CreatedAtAction(nameof(ObtenerPorId), new { id = nueva.IdCategoria }, respuesta);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaDto>> Actualizar(int id, [FromBody] ActualizarCategoriaDto dto, CancellationToken cancellationToken)
    {
        var categoria = await _context.Categorias.FindAsync(new object[] { id }, cancellationToken);
        if (categoria == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Categoría no encontrada",
                Detail = $"No existe una categoría con Id {id}."
            });
        }

        var duplicado = await _context.Categorias
            .AnyAsync(c => c.IdCategoria != id && c.Nombre.ToLower() == dto.Nombre.Trim().ToLower(), cancellationToken);

        if (duplicado)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Nombre en uso",
                Detail = $"Ya existe otra categoría con el nombre '{dto.Nombre.Trim()}'."
            });
        }

        categoria.Nombre = dto.Nombre.Trim();
        categoria.Descripcion = dto.Descripcion?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new CategoriaDto
        {
            IdCategoria = categoria.IdCategoria,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            FechaCreacion = categoria.FechaCreacion
        };

        return Ok(respuesta);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var categoria = await _context.Categorias.FindAsync(new object[] { id }, cancellationToken);
        if (categoria == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Categoría no encontrada",
                Detail = $"No existe una categoría con Id {id}."
            });
        }

        // Verificación de integridad referencial física (Restrict)
        var tieneProductos = await _context.Productos.AnyAsync(p => p.IdCategoria == id, cancellationToken);
        if (tieneProductos)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Restricción de integridad referencial",
                Detail = $"No se puede eliminar la categoría '{categoria.Nombre}' (Id: {id}) porque tiene productos asociados. Regla física configurada: Restrict."
            });
        }

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
