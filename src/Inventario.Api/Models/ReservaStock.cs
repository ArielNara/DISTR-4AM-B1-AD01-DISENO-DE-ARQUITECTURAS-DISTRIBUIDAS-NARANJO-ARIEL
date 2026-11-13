namespace Inventario.Api.Models;

public class ReservaStock
{
    public int IdReserva { get; set; }

    // Referencia lógica inter-servicio hacia Catalogo.Api (NO FK física)
    public int IdProducto { get; set; }

    // FK física intra-BD hacia Bodega
    public int IdBodega { get; set; }
    public int Cantidad { get; set; }
    public string Estado { get; set; } = "ACTIVA"; // ACTIVA | CONFIRMADA | CANCELADA
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public string? ReferenciaExterna { get; set; }

    public Bodega? Bodega { get; set; }
}
