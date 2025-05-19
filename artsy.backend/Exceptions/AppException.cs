namespace artsy.backend.Exceptions;

public class AppException : Exception
{
	protected AppException() : base() { }
	protected AppException(string message) : base(message) { }
	protected AppException(string message, Exception innerException) : base(message, innerException) { }
}
