namespace PhotoPrint.API.Exceptions;

public interface IErrorCoded
{
    string ErrorCode { get; }
}
