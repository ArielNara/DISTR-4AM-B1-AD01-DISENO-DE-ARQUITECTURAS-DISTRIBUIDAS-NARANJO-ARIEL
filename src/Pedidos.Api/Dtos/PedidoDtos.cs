using System.ComponentModel.DataAnnotations;

namespace Pedidos.Api.Dtos;

public class PedidoFiltrosDto
{
    public int? IdCliente { get; set; }
    public string? Estado { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

public class DetallePedidoInputDto
{
    [Required(ErrorMessage = "El identificador del producto es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador del producto no es válido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "La bodega es obligatoria para reservar stock.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de la bodega no es válido.")]
    public int IdBodega { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}

public class CrearPedidoDto
{
    [Required(ErrorMessage = "Debe indicar el cliente que realiza el pedido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador del cliente no es válido.")]
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "Debe incluir al menos un detalle en el pedido.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un detalle en el pedido.")]
    public List<DetallePedidoInputDto> Detalles { get; set; } = new();
}

public class PedidoDto
{
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class DetallePedidoDto
{
    public int IdDetalle { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int? IdReservaInventario { get; set; }
}

public class PedidoDetalleDto
{
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<DetallePedidoDto> Detalles { get; set; } = new();
}
