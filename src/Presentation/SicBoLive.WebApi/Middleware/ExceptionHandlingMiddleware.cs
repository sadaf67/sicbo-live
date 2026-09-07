namespace SicBoLive.WebApi.Middleware;

/// <summary>
/// Catches exceptions thrown anywhere downstream (domain guard clauses, MediatR handlers) and
/// returns just the exception's (Persian) message as a plain-text body with an appropriate status
/// code — instead of letting ASP.NET Core's developer exception page dump the full English stack
/// trace to the client. The frontend already expects a plain string response body on error.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError,
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";

            var message = statusCode == StatusCodes.Status500InternalServerError
                ? "خطای غیرمنتظره‌ای رخ داد. لطفاً دوباره تلاش کنید."
                : ex.Message;

            await context.Response.WriteAsync(message);
        }
    }
}
