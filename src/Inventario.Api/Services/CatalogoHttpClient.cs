using System.Net;
using System.Text.Json;

namespace Inventario.Api.Services;

public class ProductoCatalogoDto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public interface ICatalogoHttpClient
{
    Task<ProductoCatalogoDto?> ObtenerProductoPorIdAsync(int idProducto, CancellationToken cancellationToken = default);
}

public class CatalogoHttpClient : ICatalogoHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogoHttpClient> _logger;

    public CatalogoHttpClient(HttpClient httpClient, ILogger<CatalogoHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductoCatalogoDto?> ObtenerProductoPorIdAsync(int idProducto, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/productos/{idProducto}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ProductoCatalogoDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Catalogo.Api al consultar producto {IdProducto}", idProducto);
            throw new ServicioNoDisponibleException("El microservicio Catalogo.Api no está disponible o no respondió a tiempo.", ex);
        }
    }
}

public class ServicioNoDisponibleException : Exception
{
    public ServicioNoDisponibleException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
