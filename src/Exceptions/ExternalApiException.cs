using System.Net;
using artsy.backend.Exceptions;

namespace artsy.backend.exceptions;

public class ExternalApiException : AppException
{
	public ExternalApiException(string serviceName, string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
		: base(message, innerException)
	{
		ServiceName = serviceName;
		StatusCode = statusCode;
	}

	public HttpStatusCode? StatusCode { get; }
	public string? ServiceName { get; }
}
