namespace Catalogo.Api.Models;

public class Marca
{
    public int IdMarca { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string PaisOrigen { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
