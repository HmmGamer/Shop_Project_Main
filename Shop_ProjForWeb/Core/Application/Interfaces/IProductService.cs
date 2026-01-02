namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Application.DTOs;

public interface IProductService
{
    Task<ProductDto> CreateProductAsync(CreateProductDto dto);
    Task UpdateProductAsync(Guid id, UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
    Task<ProductDto> GetProductAsync(Guid id);
    Task<List<ProductDto>> GetAllProductsAsync();
    Task<List<ProductDto>> SearchProductsAsync(string name);
}
