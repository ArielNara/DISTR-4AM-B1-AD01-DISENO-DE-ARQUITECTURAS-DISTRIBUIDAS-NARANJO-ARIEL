using System.ComponentModel.DataAnnotations;

namespace Inventario.Api.Dtos;

public class ExistenciaDto
{
    public int IdExistencia { get; set; }
    public int IdProducto { get; set; }
    public int IdBodega { get; set; }
    public string? BodegaNombre { get; set; }
    public string? BodegaCiudad { get; set; }
    public int Stock { get; set; }
    public int ReservasActivas { get; set; }
    public int StockDisponible => Math.Max(0, Stock - ReservasActivas);
    public DateTime UltimaActualizacion { get; set; }
}

public class BodegaStockDto
{
    public int IdBodega { get; set; }
    public string BodegaNombre { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public int StockFisico { get; set; }
    public int ReservasActivas { get; set; }
    public int StockDisponible => Math.Max(0, StockFisico - ReservasActivas);
}

public class ExistenciaResumenProductoDto
{
    public int IdProducto { get; set; }
    public int TotalStockFisico { get; set; }
    public int TotalReservasActivas { get; set; }
    public int TotalStockDisponible => Math.Max(0, TotalStockFisico - TotalReservasActivas);
    public IEnumerable<BodegaStockDto> Bodegas { get; set; } = new List<BodegaStockDto>();
}

public class AjustarExistenciaDto
{
    [Required(ErrorMessage = "El identificador de producto es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de producto no es válido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El identificador de bodega es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de bodega no es válido.")]
    public int IdBodega { get; set; }

    [Required(ErrorMessage = "El nuevo stock es obligatorio.")]
    [Range(0, 1000000, ErrorMessage = "El stock debe ser un valor entero no negativo.")]
    public int NuevoStock { get; set; }

    [StringLength(250, ErrorMessage = "Las observaciones no pueden superar los 250 caracteres.")]
    public string? Motivo { get; set; }
}

public class ExistenciaFiltrosDto
{
    public int? IdProducto { get; set; }
    public int? IdBodega { get; set; }
}
