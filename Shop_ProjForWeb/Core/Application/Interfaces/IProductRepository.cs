namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Domain.Entities;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetAllAsync();
    Task UpdateAsync(Product product);
    Task AddAsync(Product product);
    Task DeleteAsync(Guid id);
    Task<List<Product>> GetActiveProductsAsync();
    Task<List<Product>> SearchAsync(string name);
    Task<bool> HasOrderItemsAsync(Guid productId);
    Task<List<Product>> GetAllIncludingDeletedAsync();
    Task<Product?> GetByIdIncludingDeletedAsync(Guid id);
    Task RestoreAsync(Guid id);
}
