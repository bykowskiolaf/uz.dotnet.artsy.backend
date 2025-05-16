namespace artsy.backend.Exceptions;

public class BadRequestException : AppException
{
	public BadRequestException() : base() { }
	public BadRequestException(string message) : base(message) { }
	public BadRequestException(string message, Exception innerException) : base(message, innerException) { }
	public object? Errors { get; set; }
}
