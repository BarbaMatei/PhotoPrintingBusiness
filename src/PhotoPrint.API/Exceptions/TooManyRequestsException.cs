namespace PhotoPrint.API.Exceptions;

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message)
    {
    }
}
