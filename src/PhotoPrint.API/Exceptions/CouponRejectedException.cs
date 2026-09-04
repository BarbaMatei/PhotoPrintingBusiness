namespace PhotoPrint.API.Exceptions;

public class CouponRejectedException : UnprocessableEntityException, IErrorCoded
{
    public CouponRejectedException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
