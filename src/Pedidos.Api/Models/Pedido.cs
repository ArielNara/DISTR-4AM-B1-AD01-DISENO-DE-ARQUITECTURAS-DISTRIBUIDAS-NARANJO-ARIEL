namespace Pedidos.Api.Models;

public class Pedido
{
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public string Estado { get; set; } = "PENDIENTE";

    public Cliente? Cliente { get; set; }
    public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
}
