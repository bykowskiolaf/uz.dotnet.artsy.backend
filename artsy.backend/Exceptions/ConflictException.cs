namespace artsy.backend.Exceptions;

public class ConflictException : AppException
{
	public ConflictException() : base() { }
	public ConflictException(string message) : base(message) { }
	public ConflictException(string message, Exception innerException) : base(message, innerException) { }
}
