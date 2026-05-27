namespace PhotoPrint.API.Exceptions;

public class UnsupportedMediaTypeException : Exception
{
    public UnsupportedMediaTypeException(string message) : base(message)
    {
    }
}
