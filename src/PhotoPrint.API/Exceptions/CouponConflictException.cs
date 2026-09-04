namespace PhotoPrint.API.Exceptions;

public class CouponConflictException : ConflictException, IErrorCoded
{
    public CouponConflictException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
