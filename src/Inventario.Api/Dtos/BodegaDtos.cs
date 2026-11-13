using System.ComponentModel.DataAnnotations;

namespace Inventario.Api.Dtos;

public class BodegaDto
{
    public int IdBodega { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class CrearBodegaDto
{
    [Required(ErrorMessage = "El nombre de la bodega es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ciudad es obligatoria.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "La ciudad debe tener entre 2 y 100 caracteres.")]
    public string Ciudad { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "La dirección debe tener entre 5 y 200 caracteres.")]
    public string Direccion { get; set; } = string.Empty;
}

public class ActualizarBodegaDto
{
    [Required(ErrorMessage = "El nombre de la bodega es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ciudad es obligatoria.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "La ciudad debe tener entre 2 y 100 caracteres.")]
    public string Ciudad { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "La dirección debe tener entre 5 y 200 caracteres.")]
    public string Direccion { get; set; } = string.Empty;
}
