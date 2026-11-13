using System.ComponentModel.DataAnnotations;

namespace Inventario.Api.Dtos;

public class MovimientoDto
{
    public int IdMovimiento { get; set; }
    public int IdProducto { get; set; }
    public int IdBodega { get; set; }
    public string? BodegaNombre { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observaciones { get; set; }
}

public class CrearMovimientoDto
{
    [Required(ErrorMessage = "El identificador de producto es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de producto no es válido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El identificador de bodega es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de bodega no es válido.")]
    public int IdBodega { get; set; }

    [Required(ErrorMessage = "El tipo de movimiento es obligatorio.")]
    [RegularExpression("^(ENTRADA|SALIDA|AJUSTE)$", ErrorMessage = "El tipo de movimiento debe ser ENTRADA, SALIDA o AJUSTE.")]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 1000000, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }

    [StringLength(250, ErrorMessage = "Las observaciones no pueden superar los 250 caracteres.")]
    public string? Observaciones { get; set; }
}

public class MovimientoFiltrosDto
{
    public int? IdProducto { get; set; }
    public int? IdBodega { get; set; }
    public string? Tipo { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
