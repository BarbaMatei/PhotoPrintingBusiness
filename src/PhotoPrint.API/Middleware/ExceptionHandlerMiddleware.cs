using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Extensions;

namespace PhotoPrint.API.Middleware;

public class ExceptionHandlerMiddleware : IMiddleware
{
    private static readonly Dictionary<Type, (int StatusCode, string Title)> _exceptionMappings = new()
    {
        [typeof(NotFoundException)]             = (StatusCodes.Status404NotFound, "Not Found"),
        [typeof(ConflictException)]             = (StatusCodes.Status409Conflict, "Conflict"),
        [typeof(IdempotencyConflictException)]  = (StatusCodes.Status409Conflict, "Idempotency conflict"),
        [typeof(IdempotencyKeyTakenException)]  = (StatusCodes.Status409Conflict, "Conflict"),
        [typeof(IdempotencyKeyConsumedException)] = (StatusCodes.Status409Conflict, "Conflict"),
        [typeof(ForbiddenException)]            = (StatusCodes.Status403Forbidden, "Forbidden"),
        [typeof(UnauthorizedException)]         = (StatusCodes.Status401Unauthorized, "Unauthorized"),
        [typeof(BadGatewayException)]           = (StatusCodes.Status502BadGateway, "Bad Gateway"),
        [typeof(BadRequestException)]           = (StatusCodes.Status400BadRequest, "Bad Request"),
        [typeof(InvalidOrderTransitionException)]= (StatusCodes.Status400BadRequest, "Bad Request"),
        [typeof(UnprocessableEntityException)]  = (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
        [typeof(DecompressionBombException)]    = (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
        // A decode that slips the pixel-area check but trips ImageSharp's allocation backstop
        // (Program.cs) throws this, not an ImageFormatException — map it to 422, not a raw 500.
        [typeof(SixLabors.ImageSharp.Memory.InvalidMemoryOperationException)] = (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
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
            // nothing to return. Emit at Information (not Debug): the Serilog minimum level
            // is Information in every environment, so a Debug line is filtered out and the
            // signal is lost entirely. Distinct low-cardinality event.
            _logger.LogInformation(
                "request.client_aborted path={Path} correlation_id={CorrelationId}",
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
        var correlationId = context.GetCorrelationId() ?? Guid.NewGuid().ToString();

        // Kestrel rejects an oversize or malformed request with this and names the status it wants; unmapped it would be a 500 plus a Sentry capture, so anyone could turn a rejected body into an error-budget burn. A 5xx it asks for still falls through, so nothing skips the capture invariant below.
        if (exception is BadHttpRequestException badRequest
            && badRequest.StatusCode < StatusCodes.Status500InternalServerError)
        {
            _logger.LogWarning(
                "request.rejected status={Status} reason={Reason} path={Path} correlation_id={CorrelationId}",
                badRequest.StatusCode, badRequest.Message, context.Request.Path, correlationId);

            var reason = ReasonPhrases.GetReasonPhrase(badRequest.StatusCode);
            await WriteProblemDetailsAsync(context, badRequest.StatusCode,
                string.IsNullOrEmpty(reason) ? "Bad Request" : reason,
                badRequest.Message, correlationId, exception);
            return;
        }

        if (_exceptionMappings.TryGetValue(exception.GetType(), out var mapping))
        {
            // A mapped status is not the same as an expected outcome: a mapped 5xx is a
            // dependency failure that burns the availability SLO, so it is keyed on the status
            // code rather than an exception list — a mapping added later cannot skip capture.
            var serverError = mapping.StatusCode >= StatusCodes.Status500InternalServerError;

            if (serverError)
                _logger.LogError(
                    exception,
                    "Handled server-side exception {ExceptionType}: {Message} | Path: {Path} | CorrelationId: {CorrelationId}",
                    exception.GetType().Name,
                    exception.Message,
                    context.Request.Path,
                    correlationId);
            else
                _logger.LogWarning(
                    "Handled exception {ExceptionType}: {Message} | Path: {Path} | CorrelationId: {CorrelationId}",
                    exception.GetType().Name,
                    exception.Message,
                    context.Request.Path,
                    correlationId);

            // Emit the structured event ddd-01:61 reserves so a
            // conflict is distinctly observable (a signal of client bugs / key-reuse abuse)
            // rather than buried in the generic warning above. Field NAMES only — no values.
            if (exception is IdempotencyConflictException conflict)
                _logger.LogWarning(
                    "payments.idempotency.conflict correlation_id={CorrelationId} divergent_fields={DivergentFields}",
                    correlationId, string.Join(",", conflict.DivergentFields));

            // A cross-tenant key collision (a borrowed/guessed key
            // or a key-squatting probe) also 409s, but via a plain ConflictException that
            // was indistinguishable from any other 409 in the logs — exactly the signal an
            // operator needs to grep during a duplicate-charge incident. Emit it as a
            // distinct reserved event (no key value — just the class + correlation id).
            if (exception is IdempotencyKeyTakenException)
                _logger.LogWarning(
                    "payments.idempotency.cross-tenant-conflict correlation_id={CorrelationId}",
                    correlationId);

            if (exception is IdempotencyKeyConsumedException consumed)
                _logger.LogWarning(
                    "payments.idempotency.key-consumed correlation_id={CorrelationId} order_id={OrderId}",
                    correlationId, consumed.OrderId);

            // A rejected decompression bomb 422s like an ordinary
            // "unreadable image" 422, so ops can't alert on a bomb spike. Emit a distinct
            // reserved event carrying the offending dimensions (no file data / no PII).
            if (exception is DecompressionBombException bomb)
                _logger.LogWarning(
                    "uploads.decompression_bomb.rejected correlation_id={CorrelationId} source=pixel_guard width={Width} height={Height}",
                    correlationId, bomb.WidthPx, bomb.HeightPx);

            // A bomb that under-reported its dimensions passes the pixel
            // guard but trips the 512 MB allocator backstop, throwing InvalidMemoryOperationException.
            // Emit the SAME reserved bomb event so ops alerting on it catch exactly the bombs that
            // evaded the primary guard — not just a generic "Handled exception" warning. No
            // dimensions are available here (the decoder never surfaced them); source distinguishes it.
            if (exception is SixLabors.ImageSharp.Memory.InvalidMemoryOperationException)
                _logger.LogWarning(
                    "uploads.decompression_bomb.rejected correlation_id={CorrelationId} source=allocator_backstop",
                    correlationId);

            if (serverError)
                context.RequestServices?.GetService<Sentry.IHub>()?.CaptureException(exception);

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

            // Sentry integration (intent 020 bolt 045): unhandled exceptions are
            // captured explicitly because Serilog replaces other logging providers
            // (intent 001) which would otherwise short-circuit Sentry's automatic
            // capture via MEL. We resolve IHub from per-request DI (rather than
            // the static SentrySdk) so each WebApplicationFactory in tests uses
            // its own hub — the static hub is process-global and shared across
            // factories.
            var hub = context.RequestServices?.GetService<Sentry.IHub>();
            hub?.CaptureException(exception);

            var detail = _environment.IsDevelopment()
                ? exception.Message
                : "A apărut o eroare neașteptată. Încearcă din nou.";

            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error", detail, correlationId,
                _environment.IsDevelopment() ? exception : null);
        }
    }

    private async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string correlationId,
        Exception? exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        // The documented 409 contract is "names the divergent
        // fields". Compute it once and surface it in BOTH the Development diagnostic shape
        // and the production ProblemDetails — previously only the prod branch carried it,
        // so a FE built against the dev API never saw the contract field. Field NAMES only,
        // never values (no PII).
        var divergentFields = (exception as IdempotencyConflictException)?.DivergentFields;
        var consumedOrderId = (exception as IdempotencyKeyConsumedException)?.OrderId;

        object response;

        // Use the injected _environment (this is now an instance
        // method) instead of re-resolving IHostEnvironment via context.RequestServices — the
        // middleware already holds it, and the service-locator hop was a second, redundant
        // way to answer the same question.
        if (exception != null && _environment.IsDevelopment())
        {
            response = new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title,
                status = statusCode,
                detail,
                correlationId,
                divergentFields,
                orderId = consumedOrderId,
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

            if (divergentFields is not null)
                problem.Extensions["divergentFields"] = divergentFields;

            if (consumedOrderId is not null)
                problem.Extensions["orderId"] = consumedOrderId.ToString();

            response = problem;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
