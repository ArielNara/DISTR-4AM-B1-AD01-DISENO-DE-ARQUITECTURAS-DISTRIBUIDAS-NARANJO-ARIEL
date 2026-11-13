namespace Inventario.Api.Models;

public class Bodega
{
    public int IdBodega { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<Existencia> Existencias { get; set; } = new List<Existencia>();
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
    public ICollection<ReservaStock> Reservas { get; set; } = new List<ReservaStock>();
}
