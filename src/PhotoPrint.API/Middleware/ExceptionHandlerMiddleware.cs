using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.Exceptions;

namespace PhotoPrint.API.Middleware;

public class ExceptionHandlerMiddleware : IMiddleware
{
    private static readonly Dictionary<Type, (int StatusCode, string Title)> _exceptionMappings = new()
    {
        [typeof(NotFoundException)]             = (StatusCodes.Status404NotFound, "Not Found"),
        [typeof(ConflictException)]             = (StatusCodes.Status409Conflict, "Conflict"),
        [typeof(IdempotencyConflictException)]  = (StatusCodes.Status409Conflict, "Idempotency conflict"),
        [typeof(ForbiddenException)]            = (StatusCodes.Status403Forbidden, "Forbidden"),
        [typeof(UnauthorizedException)]         = (StatusCodes.Status401Unauthorized, "Unauthorized"),
        [typeof(BadGatewayException)]           = (StatusCodes.Status502BadGateway, "Bad Gateway"),
        [typeof(BadRequestException)]           = (StatusCodes.Status400BadRequest, "Bad Request"),
        [typeof(InvalidOrderTransitionException)]= (StatusCodes.Status400BadRequest, "Bad Request"),
        [typeof(UnprocessableEntityException)]  = (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
        [typeof(UnsupportedMediaTypeException)] = (StatusCodes.Status415UnsupportedMediaType, "Unsupported Media Type"),
        [typeof(RequestEntityTooLargeException)]= (StatusCodes.Status413RequestEntityTooLarge, "Request Entity Too Large"),
        [typeof(TooManyRequestsException)]      = (StatusCodes.Status429TooManyRequests, "Too Many Requests"),
    };

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlerMiddleware(
        ILogger<ExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected/cancelled mid-request (e.g. navigated away or
            // reloaded). That's not a server error — the caller is gone, so there's
            // nothing to return; log quietly instead of as an unhandled 500.
            _logger.LogDebug(
                "Request {Path} was cancelled by the client. | CorrelationId: {CorrelationId}",
                context.Request.Path,
                context.Items["CorrelationId"]?.ToString());
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        if (_exceptionMappings.TryGetValue(exception.GetType(), out var mapping))
        {
            _logger.LogWarning(
                "Handled exception {ExceptionType}: {Message} | Path: {Path} | CorrelationId: {CorrelationId}",
                exception.GetType().Name,
                exception.Message,
                context.Request.Path,
                correlationId);

            await WriteProblemDetailsAsync(context, mapping.StatusCode, mapping.Title,
                exception.Message, correlationId, exception);
        }
        else
        {
            _logger.LogError(
                exception,
                "Unhandled exception {ExceptionType}: {Message} | Path: {Path} | CorrelationId: {CorrelationId}",
                exception.GetType().Name,
                exception.Message,
                context.Request.Path,
                correlationId);

            var detail = _environment.IsDevelopment()
                ? exception.Message
                : "A apărut o eroare neașteptată. Încearcă din nou.";

            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error", detail, correlationId,
                _environment.IsDevelopment() ? exception : null);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string correlationId,
        Exception? exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        object response;

        if (exception != null && IsDevContext(context))
        {
            response = new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title,
                status = statusCode,
                detail,
                correlationId,
                exception = new
                {
                    type = exception.GetType().FullName,
                    message = exception.Message,
                    stackTrace = exception.StackTrace,
                },
            };
        }
        else
        {
            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = title,
                Status = statusCode,
                Detail = detail,
                Extensions = { ["correlationId"] = correlationId },
            };

            // Surface idempotency divergent field NAMES (never values — no PII).
            if (exception is IdempotencyConflictException idempotencyConflict)
                problem.Extensions["divergentFields"] = idempotencyConflict.DivergentFields;

            response = problem;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private static bool IsDevContext(HttpContext context)
    {
        var env = context.RequestServices.GetService<IHostEnvironment>();
        return env?.IsDevelopment() ?? false;
    }
}
