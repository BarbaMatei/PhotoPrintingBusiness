using PhotoPrint.API.DTOs.Products;

namespace PhotoPrint.API.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PriceCalculationResponse?> CalculatePriceAsync(Guid productId, Guid sizeId, int quantity, CancellationToken cancellationToken = default);
}
