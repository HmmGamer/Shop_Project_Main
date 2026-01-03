namespace Shop_ProjForWeb.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;

public class ProductRepository(SupermarketDbContext context) : IProductRepository
{
    private readonly SupermarketDbContext _context = context;

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            product.SoftDelete();
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Product>> GetAllIncludingDeletedAsync()
    {
        return await _context.Products.IgnoreQueryFilters().ToListAsync();
    }

    public async Task<Product?> GetByIdIncludingDeletedAsync(Guid id)
    {
        return await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task RestoreAsync(Guid id)
    {
        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
        if (product != null && product.IsDeleted)
        {
            product.IsDeleted = false;
            product.DeletedAt = null;
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Product>> GetActiveProductsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync();
    }

    public async Task<List<Product>> SearchAsync(string name)
    {
        return await _context.Products
            .Where(p => p.Name.Contains(name))
            .ToListAsync();
    }

    public async Task<bool> HasOrderItemsAsync(Guid productId)
    {
        return await _context.OrderItems
            .AnyAsync(oi => oi.ProductId == productId);
    }
}
