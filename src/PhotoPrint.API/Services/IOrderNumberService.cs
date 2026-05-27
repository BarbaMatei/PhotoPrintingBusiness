namespace PhotoPrint.API.Services;

public interface IOrderNumberService
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
