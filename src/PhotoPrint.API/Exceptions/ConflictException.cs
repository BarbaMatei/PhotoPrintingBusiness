namespace PhotoPrint.API.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
