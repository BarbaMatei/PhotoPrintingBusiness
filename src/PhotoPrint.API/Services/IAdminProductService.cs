using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.DTOs.Products;

namespace PhotoPrint.API.Services;

public interface IAdminProductService
{
    Task<List<ProductDto>> GetAllProductsAsync(CancellationToken ct = default);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task SetProductStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductSizeDto> AddProductSizeAsync(Guid productId, CreateProductSizeRequest request, CancellationToken cancellationToken = default);
    Task SetSizeStatusAsync(Guid productId, Guid sizeId, bool isActive, CancellationToken cancellationToken = default);
    Task<ProductSizeDto> ReplacePricingTiersAsync(Guid productId, Guid sizeId, ReplacePricingTiersRequest request, CancellationToken cancellationToken = default);
    Task ReplaceFinishesAsync(Guid productId, IReadOnlyList<string> names, CancellationToken ct = default);
}
