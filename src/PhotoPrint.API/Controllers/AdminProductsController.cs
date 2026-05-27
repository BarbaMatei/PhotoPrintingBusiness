using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.DTOs.Products;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController(IAdminProductService adminProductService) : ControllerBase
{
    // GET /api/admin/products
    [HttpGet]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProductsAsync(CancellationToken cancellationToken)
    {
        var products = await adminProductService.GetAllProductsAsync(cancellationToken);
        return Ok(products);
    }

    // POST /api/admin/products
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateProductAsync(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await adminProductService.CreateProductAsync(request, cancellationToken);
        return Created($"/api/admin/products/{product.Id}", product);
    }

    // PUT /api/admin/products/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProductAsync(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await adminProductService.UpdateProductAsync(id, request, cancellationToken);
        return Ok(product);
    }

    // PATCH /api/admin/products/{id}/status
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetProductStatusAsync(
        Guid id,
        [FromBody] SetStatusRequest request,
        CancellationToken cancellationToken)
    {
        await adminProductService.SetProductStatusAsync(id, request.IsActive, cancellationToken);
        return Ok(new { id, isActive = request.IsActive });
    }

    // DELETE /api/admin/products/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductAsync(Guid id, CancellationToken cancellationToken)
    {
        await adminProductService.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }

    // POST /api/admin/products/{id}/sizes
    [HttpPost("{id:guid}/sizes")]
    [ProducesResponseType(typeof(ProductSizeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddProductSizeAsync(
        Guid id,
        [FromBody] CreateProductSizeRequest request,
        CancellationToken cancellationToken)
    {
        var size = await adminProductService.AddProductSizeAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, size);
    }

    // PATCH /api/admin/products/{id}/sizes/{sizeId}/status
    [HttpPatch("{id:guid}/sizes/{sizeId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetSizeStatusAsync(
        Guid id,
        Guid sizeId,
        [FromBody] SetStatusRequest request,
        CancellationToken cancellationToken)
    {
        await adminProductService.SetSizeStatusAsync(id, sizeId, request.IsActive, cancellationToken);
        return Ok(new { id = sizeId, isActive = request.IsActive });
    }

    // PUT /api/admin/products/{id}/sizes/{sizeId}/pricing
    [HttpPut("{id:guid}/sizes/{sizeId:guid}/pricing")]
    [ProducesResponseType(typeof(ProductSizeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReplacePricingTiersAsync(
        Guid id,
        Guid sizeId,
        [FromBody] ReplacePricingTiersRequest request,
        CancellationToken cancellationToken)
    {
        var size = await adminProductService.ReplacePricingTiersAsync(id, sizeId, request, cancellationToken);
        return Ok(size);
    }

    // PUT /api/admin/products/{id}/finishes
    [HttpPut("{id:guid}/finishes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceFinishesAsync(
        Guid id,
        [FromBody] ReplaceFinishesRequest request,
        CancellationToken cancellationToken)
    {
        await adminProductService.ReplaceFinishesAsync(id, request.Names, cancellationToken);
        return NoContent();
    }
}
