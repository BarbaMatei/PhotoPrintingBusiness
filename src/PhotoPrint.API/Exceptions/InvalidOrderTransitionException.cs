namespace PhotoPrint.API.Exceptions;

public class InvalidOrderTransitionException : Exception
{
    public InvalidOrderTransitionException(string from, string to)
        : base($"Tranziția de la '{from}' la '{to}' nu este permisă.")
    {
    }
}
