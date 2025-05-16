namespace artsy.backend.Exceptions;

public class UnauthorizedException : AppException
{
	public UnauthorizedException() : base() { }
	public UnauthorizedException(string message) : base(message) { }
	public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
}
