using System.Net;
using System.Text.Json;

namespace UsuarioApi.Middleware;

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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Regla de negocio violada.");
            await EscribirRespuestaAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado procesando {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
            await EscribirRespuestaAsync(context, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado. Intente nuevamente más tarde.");
        }
    }

    private static async Task EscribirRespuestaAsync(HttpContext context, HttpStatusCode statusCode, string mensaje)
    {
        if (context.Response.HasStarted) return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { mensaje });
        await context.Response.WriteAsync(payload);
    }
}
