namespace Pedidos.Api.Models;

public class DetallePedido
{
    public int IdDetalle { get; set; }
    public int IdPedido { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int? IdReservaInventario { get; set; }

    public Pedido? Pedido { get; set; }
}
