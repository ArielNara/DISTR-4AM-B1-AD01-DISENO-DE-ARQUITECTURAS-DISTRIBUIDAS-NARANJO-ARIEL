using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pedidos.Api.Services;

namespace Pedidos.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió una excepción no controlada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case ServicioNoDisponibleException servicioEx:
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                problemDetails.Status = StatusCodes.Status503ServiceUnavailable;
                problemDetails.Title = "Servicio no disponible";
                problemDetails.Detail = servicioEx.Message;
                break;

            case KeyNotFoundException keyNotFound:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "Recurso no encontrado";
                problemDetails.Detail = keyNotFound.Message;
                break;

            case DbUpdateException dbEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Title = "Conflicto de concurrencia o integridad de datos";

                if (dbEx.InnerException is SqlException sqlEx)
                {
                    if (sqlEx.Number == 547)
                    {
                        problemDetails.Detail = "No es posible completar la operación porque el registro tiene dependencias o viola una regla de integridad referencial.";
                    }
                    else if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                    {
                        problemDetails.Detail = "Ya existe un registro con los mismos datos únicos (por ejemplo cédula o correo duplicado).";
                    }
                    else
                    {
                        problemDetails.Detail = "Error en la base de datos al intentar procesar la instrucción.";
                    }
                }
                else
                {
                    problemDetails.Detail = dbEx.Message;
                }
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Solicitud inválida";
                problemDetails.Detail = argEx.Message;
                break;

            case InvalidOperationException invEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Title = "Conflicto en la operación de negocio";
                problemDetails.Detail = invEx.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Error interno del servidor";
                problemDetails.Detail = "Se ha producido un error inesperado al procesar la solicitud.";
                break;
        }

        var json = JsonSerializer.Serialize(problemDetails);
        return context.Response.WriteAsync(json);
    }
}
