namespace Inventario.Api.Models;

public class MovimientoInventario
{
    public int IdMovimiento { get; set; }

    // Referencia lógica inter-servicio hacia Catalogo.Api (NO FK física)
    public int IdProducto { get; set; }

    // FK física intra-BD hacia Bodega
    public int IdBodega { get; set; }
    public string Tipo { get; set; } = string.Empty; // ENTRADA | SALIDA | AJUSTE
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }

    public Bodega? Bodega { get; set; }
}
