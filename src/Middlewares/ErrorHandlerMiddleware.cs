using System.Net;
using System.Text.Json;
using artsy.backend.exceptions;
using artsy.backend.Exceptions;

namespace artsy.backend.Middlewares;

public class ErrorHandlerMiddleware
{
	readonly IHostEnvironment _env;
	readonly ILogger<ErrorHandlerMiddleware> _logger;
	readonly RequestDelegate _next;

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
				case ExternalApiException e: // External API errors
					_logger.LogWarning("External API '{ServiceName}' failed with status {StatusCode}: {Message}", e.ServiceName, e.StatusCode, e.Message);
					response.StatusCode = (int)HttpStatusCode.InternalServerError;
					errorResponse = new { message = $"There was a problem communicating with an external art service ({e.ServiceName}). Please try again later.", traceId = context.TraceIdentifier };

					break;
				case AppException e: // Catch-all
					response.StatusCode = (int)HttpStatusCode.BadRequest;

					break;
				default:
					_logger.LogError(error, "An unhandled error occurred: {ErrorMessage}", error.Message);
					response.StatusCode = (int)HttpStatusCode.InternalServerError;
					errorResponse = new
					{
						message = _env.IsDevelopment() || _env.EnvironmentName == "Local" ? error.Message : "An unexpected error occurred. Please try again later.",
						traceId = context.TraceIdentifier
					};

					break;
			}

			var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
			await response.WriteAsync(result);
		}
	}
}
