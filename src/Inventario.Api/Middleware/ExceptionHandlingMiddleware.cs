using System.Net;
using System.Text.Json;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Middleware;

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
            _logger.LogError(ex, "Ocurrió una excepción en Inventario.Api: {Message}", ex.Message);
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
            case ServicioNoDisponibleException srvEx:
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                problemDetails.Status = (int)HttpStatusCode.ServiceUnavailable;
                problemDetails.Title = "Servicio dependiente no disponible";
                problemDetails.Detail = srvEx.Message;
                break;

            case KeyNotFoundException knf:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "Recurso no encontrado";
                problemDetails.Detail = knf.Message;
                break;

            case DbUpdateException dbEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Title = "Conflicto de integridad de datos";

                if (dbEx.InnerException is SqlException sqlEx)
                {
                    if (sqlEx.Number == 547) // Foreign Key constraint violation
                    {
                        problemDetails.Detail = "No es posible completar la operación porque el registro tiene dependencias en inventario (existencias, movimientos o reservas). Regla física: Restrict.";
                    }
                    else if (sqlEx.Number == 2601 || sqlEx.Number == 2627) // Unique constraint
                    {
                        problemDetails.Detail = "Ya existe un registro con la misma combinación única (por ejemplo ya existe existencia para este producto en esta bodega).";
                    }
                    else
                    {
                        problemDetails.Detail = "Error de base de datos en la instrucción ejecutada.";
                    }
                }
                else
                {
                    problemDetails.Detail = dbEx.Message;
                }
                break;

            case InvalidOperationException invEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Title = "Conflicto en la operación de inventario";
                problemDetails.Detail = invEx.Message;
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Solicitud inválida";
                problemDetails.Detail = argEx.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Error interno del servidor";
                problemDetails.Detail = "Se ha producido un error inesperado en el servicio de inventario.";
                break;
        }

        var json = JsonSerializer.Serialize(problemDetails);
        return context.Response.WriteAsync(json);
    }
}
