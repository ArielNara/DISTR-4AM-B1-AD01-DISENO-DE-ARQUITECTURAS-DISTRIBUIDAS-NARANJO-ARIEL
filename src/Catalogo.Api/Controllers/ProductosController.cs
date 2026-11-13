using Catalogo.Api.Data;
using Catalogo.Api.Dtos;
using Catalogo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Controllers;

[ApiController]
[Route("api/productos")]
[Produces("application/json")]
public class ProductosController : ControllerBase
{
    private readonly CatalogoDbContext _context;

    public ProductosController(CatalogoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginacionResultadoDto<ProductoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginacionResultadoDto<ProductoDto>>> ObtenerTodos(
        [FromQuery] ProductoFiltrosDto filtros,
        CancellationToken cancellationToken)
    {
        var query = _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.Marca)
            .AsQueryable();

        if (filtros.IdCategoria.HasValue)
        {
            query = query.Where(p => p.IdCategoria == filtros.IdCategoria.Value);
        }

        if (filtros.IdMarca.HasValue)
        {
            query = query.Where(p => p.IdMarca == filtros.IdMarca.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtros.Estado))
        {
            query = query.Where(p => p.Estado == filtros.Estado.Trim().ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(filtros.Buscar))
        {
            var termino = filtros.Buscar.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(termino) || p.SKU.ToLower().Contains(termino));
        }

        var total = await query.CountAsync(cancellationToken);

        var elementos = await query
            .OrderBy(p => p.IdProducto)
            .Skip((filtros.Pagina - 1) * filtros.TamanoPagina)
            .Take(filtros.TamanoPagina)
            .Select(p => new ProductoDto
            {
                IdProducto = p.IdProducto,
                IdCategoria = p.IdCategoria,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                IdMarca = p.IdMarca,
                MarcaNombre = p.Marca != null ? p.Marca.Nombre : null,
                Nombre = p.Nombre,
                SKU = p.SKU,
                Precio = p.Precio,
                Estado = p.Estado,
                FechaCreacion = p.FechaCreacion
            })
            .ToListAsync(cancellationToken);

        return Ok(new PaginacionResultadoDto<ProductoDto>
        {
            TotalRegistros = total,
            Pagina = filtros.Pagina,
            TamanoPagina = filtros.TamanoPagina,
            Elementos = elementos
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoDto>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.Marca)
            .Where(p => p.IdProducto == id)
            .Select(p => new ProductoDto
            {
                IdProducto = p.IdProducto,
                IdCategoria = p.IdCategoria,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                IdMarca = p.IdMarca,
                MarcaNombre = p.Marca != null ? p.Marca.Nombre : null,
                Nombre = p.Nombre,
                SKU = p.SKU,
                Precio = p.Precio,
                Estado = p.Estado,
                FechaCreacion = p.FechaCreacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (producto == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Producto no encontrado",
                Detail = $"No existe un producto con Id {id}."
            });
        }

        return Ok(producto);
    }

    [HttpGet("sku/{sku}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoDto>> ObtenerPorSku(string sku, CancellationToken cancellationToken)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.Marca)
            .Where(p => p.SKU.ToLower() == sku.Trim().ToLower())
            .Select(p => new ProductoDto
            {
                IdProducto = p.IdProducto,
                IdCategoria = p.IdCategoria,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                IdMarca = p.IdMarca,
                MarcaNombre = p.Marca != null ? p.Marca.Nombre : null,
                Nombre = p.Nombre,
                SKU = p.SKU,
                Precio = p.Precio,
                Estado = p.Estado,
                FechaCreacion = p.FechaCreacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (producto == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Producto no encontrado",
                Detail = $"No existe un producto con SKU '{sku}'."
            });
        }

        return Ok(producto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductoDto>> Crear([FromBody] CrearProductoDto dto, CancellationToken cancellationToken)
    {
        // Validar categoría existente
        var existeCategoria = await _context.Categorias.AnyAsync(c => c.IdCategoria == dto.IdCategoria, cancellationToken);
        if (!existeCategoria)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Categoría no válida",
                Detail = $"No existe una categoría con Id {dto.IdCategoria}."
            });
        }

        // Validar marca existente
        var existeMarca = await _context.Marcas.AnyAsync(m => m.IdMarca == dto.IdMarca, cancellationToken);
        if (!existeMarca)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Marca no válida",
                Detail = $"No existe una marca con Id {dto.IdMarca}."
            });
        }

        // Validar SKU único
        var existeSku = await _context.Productos.AnyAsync(p => p.SKU.ToLower() == dto.SKU.Trim().ToLower(), cancellationToken);
        if (existeSku)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "SKU duplicado",
                Detail = $"Ya existe un producto con el SKU '{dto.SKU.Trim()}'."
            });
        }

        var nuevo = new Producto
        {
            IdCategoria = dto.IdCategoria,
            IdMarca = dto.IdMarca,
            Nombre = dto.Nombre.Trim(),
            SKU = dto.SKU.Trim().ToUpper(),
            Precio = dto.Precio,
            Estado = dto.Estado.Trim().ToUpper(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.Productos.Add(nuevo);
        await _context.SaveChangesAsync(cancellationToken);

        // Cargar nombres de relaciones para la respuesta
        var categoria = await _context.Categorias.FindAsync(new object[] { nuevo.IdCategoria }, cancellationToken);
        var marca = await _context.Marcas.FindAsync(new object[] { nuevo.IdMarca }, cancellationToken);

        var respuesta = new ProductoDto
        {
            IdProducto = nuevo.IdProducto,
            IdCategoria = nuevo.IdCategoria,
            CategoriaNombre = categoria?.Nombre,
            IdMarca = nuevo.IdMarca,
            MarcaNombre = marca?.Nombre,
            Nombre = nuevo.Nombre,
            SKU = nuevo.SKU,
            Precio = nuevo.Precio,
            Estado = nuevo.Estado,
            FechaCreacion = nuevo.FechaCreacion
        };

        return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevo.IdProducto }, respuesta);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductoDto>> Actualizar(int id, [FromBody] ActualizarProductoDto dto, CancellationToken cancellationToken)
    {
        var producto = await _context.Productos.FindAsync(new object[] { id }, cancellationToken);
        if (producto == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Producto no encontrado",
                Detail = $"No existe un producto con Id {id}."
            });
        }

        // Validar categoría existente
        var existeCategoria = await _context.Categorias.AnyAsync(c => c.IdCategoria == dto.IdCategoria, cancellationToken);
        if (!existeCategoria)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Categoría no válida",
                Detail = $"No existe una categoría con Id {dto.IdCategoria}."
            });
        }

        // Validar marca existente
        var existeMarca = await _context.Marcas.AnyAsync(m => m.IdMarca == dto.IdMarca, cancellationToken);
        if (!existeMarca)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Marca no válida",
                Detail = $"No existe una marca con Id {dto.IdMarca}."
            });
        }

        // Validar SKU único contra otros registros
        var duplicadoSku = await _context.Productos
            .AnyAsync(p => p.IdProducto != id && p.SKU.ToLower() == dto.SKU.Trim().ToLower(), cancellationToken);

        if (duplicadoSku)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "SKU en uso",
                Detail = $"Ya existe otro producto con el SKU '{dto.SKU.Trim()}'."
            });
        }

        producto.IdCategoria = dto.IdCategoria;
        producto.IdMarca = dto.IdMarca;
        producto.Nombre = dto.Nombre.Trim();
        producto.SKU = dto.SKU.Trim().ToUpper();
        producto.Precio = dto.Precio;
        producto.Estado = dto.Estado.Trim().ToUpper();

        await _context.SaveChangesAsync(cancellationToken);

        var categoria = await _context.Categorias.FindAsync(new object[] { producto.IdCategoria }, cancellationToken);
        var marca = await _context.Marcas.FindAsync(new object[] { producto.IdMarca }, cancellationToken);

        var respuesta = new ProductoDto
        {
            IdProducto = producto.IdProducto,
            IdCategoria = producto.IdCategoria,
            CategoriaNombre = categoria?.Nombre,
            IdMarca = producto.IdMarca,
            MarcaNombre = marca?.Nombre,
            Nombre = producto.Nombre,
            SKU = producto.SKU,
            Precio = producto.Precio,
            Estado = producto.Estado,
            FechaCreacion = producto.FechaCreacion
        };

        return Ok(respuesta);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var producto = await _context.Productos.FindAsync(new object[] { id }, cancellationToken);
        if (producto == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Producto no encontrado",
                Detail = $"No existe un producto con Id {id}."
            });
        }

        // Baja lógica explícita requerida por la especificación
        producto.Estado = "INACTIVO";
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
