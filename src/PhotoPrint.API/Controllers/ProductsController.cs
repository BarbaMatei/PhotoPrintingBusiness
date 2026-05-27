using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs.Products;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/products")]
[AllowAnonymous]
public class ProductsController(IProductService productService) : ControllerBase
{
    // GET /api/products
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var products = await productService.GetCatalogAsync(cancellationToken);
        return Ok(products);
    }

    // GET /api/products/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await productService.GetActiveByIdAsync(id, cancellationToken);
        if (product is null)
            return NotFound();
        return Ok(product);
    }

    // GET /api/products/{id}/sizes/{sizeId}/price?quantity=N
    [HttpGet("{id:guid}/sizes/{sizeId:guid}/price")]
    [ProducesResponseType(typeof(PriceCalculationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculatePriceAsync(
        Guid id,
        Guid sizeId,
        [FromQuery] int quantity,
        CancellationToken cancellationToken)
    {
        if (quantity < 1 || quantity > 9999)
            return ValidationProblem(new ValidationProblemDetails
            {
                Errors = { ["quantity"] = ["Quantity must be between 1 and 9999."] }
            });

        var result = await productService.CalculatePriceAsync(id, sizeId, quantity, cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
}
