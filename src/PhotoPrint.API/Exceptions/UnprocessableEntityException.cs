namespace PhotoPrint.API.Exceptions;

public class UnprocessableEntityException : Exception
{
    public UnprocessableEntityException(string message) : base(message)
    {
    }

    public UnprocessableEntityException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
