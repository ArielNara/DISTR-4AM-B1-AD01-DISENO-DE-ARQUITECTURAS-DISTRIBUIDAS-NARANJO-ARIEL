namespace Inventario.Api.Models;

public class Existencia
{
    public int IdExistencia { get; set; }

    // Referencia lógica inter-servicio hacia Catalogo.Api (NO FK física)
    public int IdProducto { get; set; }

    // FK física intra-BD hacia Bodega
    public int IdBodega { get; set; }
    public int Stock { get; set; }
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;

    public Bodega? Bodega { get; set; }
}
