using System.ComponentModel.DataAnnotations;

namespace Catalogo.Api.Dtos;

public class MarcaDto
{
    public int IdMarca { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string PaisOrigen { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class CrearMarcaDto
{
    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El país de origen es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El país de origen debe tener entre 2 y 100 caracteres.")]
    public string PaisOrigen { get; set; } = string.Empty;
}

public class ActualizarMarcaDto
{
    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El país de origen es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El país de origen debe tener entre 2 y 100 caracteres.")]
    public string PaisOrigen { get; set; } = string.Empty;
}
