using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos.Api.Data;
using Pedidos.Api.Dtos;
using Pedidos.Api.Models;

namespace Pedidos.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly PedidosDbContext _context;

    public ClientesController(PedidosDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClienteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClienteDto>>> ObtenerTodos(CancellationToken cancellationToken)
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .OrderBy(c => c.IdCliente)
            .Select(c => new ClienteDto
            {
                IdCliente = c.IdCliente,
                Nombres = c.Nombres,
                Cedula = c.Cedula,
                Correo = c.Correo,
                FechaCreacion = c.FechaCreacion
            })
            .ToListAsync(cancellationToken);

        return Ok(clientes);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.IdCliente == id)
            .Select(c => new ClienteDto
            {
                IdCliente = c.IdCliente,
                Nombres = c.Nombres,
                Cedula = c.Cedula,
                Correo = c.Correo,
                FechaCreacion = c.FechaCreacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cliente == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Cliente no encontrado",
                Detail = $"No existe un cliente con Id {id}."
            });
        }

        return Ok(cliente);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClienteDto>> Crear([FromBody] CrearClienteDto dto, CancellationToken cancellationToken)
    {
        if (await _context.Clientes.AnyAsync(c => c.Cedula == dto.Cedula.Trim(), cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Cédula duplicada",
                Detail = $"Ya existe un cliente registrado con la cédula '{dto.Cedula.Trim()}'."
            });
        }

        if (await _context.Clientes.AnyAsync(c => c.Correo.ToLower() == dto.Correo.Trim().ToLower(), cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Correo duplicado",
                Detail = $"Ya existe un cliente registrado con el correo '{dto.Correo.Trim()}'."
            });
        }

        var nuevo = new Cliente
        {
            Nombres = dto.Nombres.Trim(),
            Cedula = dto.Cedula.Trim(),
            Correo = dto.Correo.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.Clientes.Add(nuevo);
        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new ClienteDto
        {
            IdCliente = nuevo.IdCliente,
            Nombres = nuevo.Nombres,
            Cedula = nuevo.Cedula,
            Correo = nuevo.Correo,
            FechaCreacion = nuevo.FechaCreacion
        };

        return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevo.IdCliente }, respuesta);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClienteDto>> Actualizar(int id, [FromBody] ActualizarClienteDto dto, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes.FindAsync(new object[] { id }, cancellationToken);
        if (cliente == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Cliente no encontrado",
                Detail = $"No existe un cliente con Id {id}."
            });
        }

        if (await _context.Clientes.AnyAsync(c => c.IdCliente != id && c.Cedula == dto.Cedula.Trim(), cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Cédula duplicada",
                Detail = $"Ya existe otro cliente con la cédula '{dto.Cedula.Trim()}'."
            });
        }

        if (await _context.Clientes.AnyAsync(c => c.IdCliente != id && c.Correo.ToLower() == dto.Correo.Trim().ToLower(), cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Correo duplicado",
                Detail = $"Ya existe otro cliente con el correo '{dto.Correo.Trim()}'."
            });
        }

        cliente.Nombres = dto.Nombres.Trim();
        cliente.Cedula = dto.Cedula.Trim();
        cliente.Correo = dto.Correo.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = new ClienteDto
        {
            IdCliente = cliente.IdCliente,
            Nombres = cliente.Nombres,
            Cedula = cliente.Cedula,
            Correo = cliente.Correo,
            FechaCreacion = cliente.FechaCreacion
        };

        return Ok(respuesta);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes.FindAsync(new object[] { id }, cancellationToken);
        if (cliente == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Cliente no encontrado",
                Detail = $"No existe un cliente con Id {id}."
            });
        }

        var tienePedidos = await _context.Pedidos.AnyAsync(p => p.IdCliente == id, cancellationToken);
        if (tienePedidos)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Integridad referencial",
                Detail = $"No se puede eliminar el cliente '{cliente.Nombres}' porque tiene pedidos asociados. Regla física configurada: Restrict."
            });
        }

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
