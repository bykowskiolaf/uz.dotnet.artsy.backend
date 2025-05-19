using System.Net;
using System.Text.Json;
using artsy.backend.Exceptions;

namespace artsy.backend.Middlewares;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            var errorResponse = new { message = error.Message, traceId = context.TraceIdentifier }; // Default error response

            switch (error)
            {
                case ConflictException e: // 409
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    break;
                case BadRequestException e: // 400
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case NotFoundException e: // 404
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                case UnauthorizedException e: // 401
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case AppException e: // Catch-all for your custom app exceptions, maybe 400 or 500
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                default: // Unhandled errors
                    _logger.LogError(error, "An unhandled error occurred: {ErrorMessage}", error.Message);
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    // For unhandled errors, don't expose error.Message directly in production if it might contain sensitive info
                    errorResponse = new {
                        message = _env.IsDevelopment() ? error.Message : "An unexpected error occurred. Please try again later.",
                        traceId = context.TraceIdentifier,
                    };
                    break;
            }

            var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await response.WriteAsync(result);
        }
    }
}