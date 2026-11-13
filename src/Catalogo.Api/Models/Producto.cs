namespace Catalogo.Api.Models;

public class Producto
{
    public int IdProducto { get; set; }
    public int IdCategoria { get; set; }
    public int IdMarca { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string Estado { get; set; } = "ACTIVO";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Categoria? Categoria { get; set; }
    public Marca? Marca { get; set; }
}
