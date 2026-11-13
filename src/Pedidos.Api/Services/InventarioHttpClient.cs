using System.Net;
using System.Text;
using System.Text.Json;

namespace Pedidos.Api.Services;

public class CrearReservaInventarioDto
{
    public int IdProducto { get; set; }
    public int IdBodega { get; set; }
    public int Cantidad { get; set; }
    public string? ReferenciaExterna { get; set; }
}

public class ReservaInventarioDto
{
    public int IdReserva { get; set; }
    public int IdProducto { get; set; }
    public int IdBodega { get; set; }
    public string BodegaNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? ReferenciaExterna { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

public interface IInventarioHttpClient
{
    Task<ReservaInventarioDto> ReservarStockAsync(CrearReservaInventarioDto dto, CancellationToken cancellationToken = default);
    Task<ReservaInventarioDto> ConfirmarReservaAsync(int idReserva, CancellationToken cancellationToken = default);
    Task<ReservaInventarioDto> CancelarReservaAsync(int idReserva, CancellationToken cancellationToken = default);
}

public class InventarioHttpClient : IInventarioHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventarioHttpClient> _logger;

    public InventarioHttpClient(HttpClient httpClient, ILogger<InventarioHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ReservaInventarioDto> ReservarStockAsync(CrearReservaInventarioDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(dto);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/reservas", content, cancellationToken);
            if ((int)response.StatusCode >= 400 && response.StatusCode != HttpStatusCode.Conflict && response.StatusCode != HttpStatusCode.BadRequest && response.StatusCode != HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(payload);
            }

            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<ReservaInventarioDto>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("La respuesta del Inventario.Api no devolvió una reserva válida.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Inventario.Api al reservar stock para producto {IdProducto} en bodega {IdBodega}", dto.IdProducto, dto.IdBodega);
            throw new ServicioNoDisponibleException("El microservicio Inventario.Api no está disponible o no respondió a tiempo.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout al reservar stock para producto {IdProducto} en bodega {IdBodega}", dto.IdProducto, dto.IdBodega);
            throw new ServicioNoDisponibleException("El microservicio Inventario.Api no respondió a tiempo.", ex);
        }
    }

    public async Task<ReservaInventarioDto> ConfirmarReservaAsync(int idReserva, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/reservas/{idReserva}/confirmar", null, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(payload);
            }

            return JsonSerializer.Deserialize<ReservaInventarioDto>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("La respuesta del Inventario.Api no devolvió una reserva válida.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Inventario.Api al confirmar la reserva {IdReserva}", idReserva);
            throw new ServicioNoDisponibleException("El microservicio Inventario.Api no está disponible o no respondió a tiempo.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout al confirmar la reserva {IdReserva}", idReserva);
            throw new ServicioNoDisponibleException("El microservicio Inventario.Api no respondió a tiempo.", ex);
        }
    }

    public async Task<ReservaInventarioDto> CancelarReservaAsync(int idReserva, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/reservas/{idReserva}/cancelar", null, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(payload);
            }

            return JsonSerializer.Deserialize<ReservaInventarioDto>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("La respuesta del Inventario.Api no devolvió una reserva válida.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Inventario.Api al cancelar la reserva {IdReserva}", idReserva);
            throw new ServicioNoDisponibleException("El microservicio Inventario.Api no está disponible o no respondió a tiempo.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout al cancelar la reserva {IdReserva}", idReserva);
            throw new ServicioNoDisponibleException("El microservicio Inventario.Api no respondió a tiempo.", ex);
        }
    }
}
