namespace artsy.backend.Exceptions;

public class NotFoundException : AppException
{
	public NotFoundException() : base() { }
	public NotFoundException(string message) : base(message) { }
	public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
