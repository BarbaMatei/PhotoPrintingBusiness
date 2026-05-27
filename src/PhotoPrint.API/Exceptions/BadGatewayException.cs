namespace PhotoPrint.API.Exceptions;

public class BadGatewayException : Exception
{
    public BadGatewayException(string message) : base(message)
    {
    }
}
