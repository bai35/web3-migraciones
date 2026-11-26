using System.Net;
using System.Text.Json;
using GimnasioApi.Excepciones;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Middlewares;

/// <summary>Manejo centralizado de errores: convierte las excepciones en respuestas JSON con el código HTTP correcto.</summary>
public class ManejoErroresMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejoErroresMiddleware> _logger;

    public ManejoErroresMiddleware(RequestDelegate siguiente, ILogger<ManejoErroresMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _siguiente(context);
        }
        catch (Exception ex)
        {
            var (codigo, mensaje) = ex switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
                AccesoProhibidoException => (HttpStatusCode.Forbidden, ex.Message),
                InvalidOperationException => (HttpStatusCode.Conflict, ex.Message),
                DbUpdateException bd when EsErrorDeIntegridad(bd) => (HttpStatusCode.Conflict, MensajeBd(bd)),
                _ => (HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.")
            };

            if (codigo == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Error no controlado");

            context.Response.StatusCode = (int)codigo;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = (int)codigo,
                error = mensaje
            }));
        }
    }

    // 2601 / 2627 = índice o clave única duplicada; 547 = violación de llave foránea.
    private static bool EsErrorDeIntegridad(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 or 547 };

    private static string MensajeBd(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 547 }
            ? "La operación viola una relación con otros datos (llave foránea)."
            : "Ya existe un registro con ese valor único (por ejemplo, documento o correo duplicado).";
}
