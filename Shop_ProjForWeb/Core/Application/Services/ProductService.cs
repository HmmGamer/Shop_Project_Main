namespace Shop_ProjForWeb.Core.Application.Services;

using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Exceptions;

public class ProductService(
    IProductRepository productRepository,
    IInventoryRepository inventoryRepository) : IProductService
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            BasePrice = dto.BasePrice,
            DiscountPercent = dto.DiscountPercent,
            IsActive = dto.IsActive
        };

        await _productRepository.AddAsync(product);

        // Create associated inventory with default threshold of 10
        var inventory = new Inventory
        {
            ProductId = product.Id,
            Quantity = dto.InitialStock,
            LowStockThreshold = 10, // Default threshold
            LowStockFlag = dto.InitialStock < 10,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _inventoryRepository.AddAsync(inventory);

        return MapToDto(product);
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new ProductNotFoundException($"Product not found with id {id}");
        }

        if (!string.IsNullOrEmpty(dto.Name))
            product.Name = dto.Name;
        if (dto.BasePrice.HasValue)
            product.BasePrice = dto.BasePrice.Value;
        if (dto.DiscountPercent.HasValue)
            product.DiscountPercent = dto.DiscountPercent.Value;
        if (dto.IsActive.HasValue)
            product.IsActive = dto.IsActive.Value;

        await _productRepository.UpdateAsync(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new ProductNotFoundException($"Product not found with id {id}");
        }

        // Check for referential integrity - products with order items cannot be deleted
        var hasOrderItems = await _productRepository.HasOrderItemsAsync(id);
        if (hasOrderItems)
        {
            throw new InvalidOperationException($"Cannot delete product that has been ordered. Consider deactivating the product instead.");
        }

        // Check if product has inventory
        var inventory = await _inventoryRepository.GetByProductIdAsync(id);
        if (inventory != null)
        {
            // Delete the inventory record (even if it has stock, since no orders exist)
            await _inventoryRepository.DeleteAsync(inventory.Id);
        }

        // Perform soft delete for audit trail
        product.SoftDelete();
        await _productRepository.UpdateAsync(product);
    }

    public async Task<ProductDto> GetProductAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new ProductNotFoundException($"Product not found with id {id}");
        }

        return MapToDto(product);
    }

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(MapToDto).ToList();
    }

    public async Task<List<ProductDto>> SearchProductsAsync(string name)
    {
        var products = await _productRepository.SearchAsync(name);
        return products.Select(MapToDto).ToList();
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            BasePrice = product.BasePrice,
            DiscountPercent = product.DiscountPercent,
            IsActive = product.IsActive,
            ImageUrl = product.ImageUrl,
            CreatedAt = product.CreatedAt
        };
    }
}
