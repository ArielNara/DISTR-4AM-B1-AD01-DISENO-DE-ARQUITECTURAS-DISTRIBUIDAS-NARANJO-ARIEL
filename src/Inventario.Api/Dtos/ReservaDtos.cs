using System.ComponentModel.DataAnnotations;

namespace Inventario.Api.Dtos;

public class ReservaStockDto
{
    public int IdReserva { get; set; }
    public int IdProducto { get; set; }
    public int IdBodega { get; set; }
    public string? BodegaNombre { get; set; }
    public int Cantidad { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? ReferenciaExterna { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

public class CrearReservaStockDto
{
    [Required(ErrorMessage = "El identificador de producto es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de producto no es válido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El identificador de bodega es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de bodega no es válido.")]
    public int IdBodega { get; set; }

    [Required(ErrorMessage = "La cantidad a reservar es obligatoria.")]
    [Range(1, 1000000, ErrorMessage = "La cantidad a reservar debe ser mayor a 0.")]
    public int Cantidad { get; set; }

    [StringLength(100, ErrorMessage = "La referencia externa no puede superar los 100 caracteres.")]
    public string? ReferenciaExterna { get; set; }
}

public class ReservaFiltrosDto
{
    public int? IdProducto { get; set; }
    public int? IdBodega { get; set; }
    public string? Estado { get; set; }
}
