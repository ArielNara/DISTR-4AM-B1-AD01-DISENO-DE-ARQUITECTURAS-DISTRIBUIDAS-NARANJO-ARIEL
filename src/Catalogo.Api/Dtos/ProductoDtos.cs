using System.ComponentModel.DataAnnotations;

namespace Catalogo.Api.Dtos;

public class PaginacionResultadoDto<T>
{
    public int TotalRegistros { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / (TamanoPagina > 0 ? TamanoPagina : 1));
    public IEnumerable<T> Elementos { get; set; } = new List<T>();
}

public class ProductoFiltrosDto
{
    public int? IdCategoria { get; set; }
    public int? IdMarca { get; set; }
    public string? Estado { get; set; }
    public string? Buscar { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor a 0.")]
    public int Pagina { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100.")]
    public int TamanoPagina { get; set; } = 10;
}

public class ProductoDto
{
    public int IdProducto { get; set; }
    public int IdCategoria { get; set; }
    public string? CategoriaNombre { get; set; }
    public int IdMarca { get; set; }
    public string? MarcaNombre { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class CrearProductoDto
{
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de categoría no es válido.")]
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de marca no es válido.")]
    public int IdMarca { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El SKU es obligatorio.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El SKU debe tener entre 3 y 50 caracteres.")]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, 9999999.99, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }

    [RegularExpression("^(ACTIVO|INACTIVO)$", ErrorMessage = "El estado debe ser ACTIVO o INACTIVO.")]
    public string Estado { get; set; } = "ACTIVO";
}

public class ActualizarProductoDto
{
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de categoría no es válido.")]
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de marca no es válido.")]
    public int IdMarca { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El SKU es obligatorio.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El SKU debe tener entre 3 y 50 caracteres.")]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, 9999999.99, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }

    [RegularExpression("^(ACTIVO|INACTIVO)$", ErrorMessage = "El estado debe ser ACTIVO o INACTIVO.")]
    public string Estado { get; set; } = "ACTIVO";
}
