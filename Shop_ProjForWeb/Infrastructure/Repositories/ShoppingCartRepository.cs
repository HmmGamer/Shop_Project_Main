using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Shop_ProjForWeb.Infrastructure.Repositories;

public class ShoppingCartRepository : IShoppingCartRepository
{
    private readonly SupermarketDbContext _context;
    private readonly ILogger<ShoppingCartRepository> _logger;

    public ShoppingCartRepository(
        SupermarketDbContext context,
        ILogger<ShoppingCartRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ShoppingCart?> GetByUserIdAsync(Guid userId)
    {
        return await _context.ShoppingCarts
            .Include(c => c.Items)
            .Where(c => c.UserId == userId && c.Status == CartStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ShoppingCart?> GetByIdAsync(Guid cartId)
    {
        return await _context.ShoppingCarts
            .FindAsync(cartId);
    }

    public async Task<ShoppingCart?> GetByIdWithItemsAsync(Guid cartId)
    {
        return await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);
    }

    public async Task<ShoppingCart> GetOrCreateCartAsync(Guid userId)
    {
        var cart = await GetByUserIdAsync(userId);
        
        if (cart == null)
        {
            cart = new ShoppingCart
            {
                UserId = userId,
                Status = CartStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.ShoppingCarts.Add(cart);
            await SaveChangesAsync();
            
            _logger.LogInformation("Created new cart for user {UserId}", userId);
        }
        
        return cart;
    }

    public async Task<List<ShoppingCart>> GetActiveCartsByUserIdAsync(Guid userId)
    {
        return await _context.ShoppingCarts
            .Include(c => c.Items)
            .Where(c => c.UserId == userId && c.Status == CartStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ShoppingCart>> GetAbandonedCartsAsync(DateTime olderThan)
    {
        return await _context.ShoppingCarts
            .Include(c => c.Items)
            .Where(c => c.Status == CartStatus.Active && c.UpdatedAt < olderThan)
            .ToListAsync();
    }

    public async Task<ShoppingCart> CreateAsync(ShoppingCart cart)
    {
        _context.ShoppingCarts.Add(cart);
        await SaveChangesAsync();
        return cart;
    }

    public async Task UpdateAsync(ShoppingCart cart)
    {
        cart.UpdatedAt = DateTime.UtcNow;
        _context.ShoppingCarts.Update(cart);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid cartId)
    {
        var cart = await GetByIdAsync(cartId);
        if (cart != null)
        {
            _context.ShoppingCarts.Remove(cart);
            await SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

public class ShoppingCartItemRepository : IShoppingCartItemRepository
{
    private readonly SupermarketDbContext _context;
    private readonly ILogger<ShoppingCartItemRepository> _logger;

    public ShoppingCartItemRepository(
        SupermarketDbContext context,
        ILogger<ShoppingCartItemRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ShoppingCartItem?> GetByIdAsync(Guid itemId)
    {
        return await _context.ShoppingCartItems
            .FindAsync(itemId);
    }

    public async Task<List<ShoppingCartItem>> GetByCartIdAsync(Guid cartId)
    {
        return await _context.ShoppingCartItems
            .Where(i => i.ShoppingCartId == cartId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<ShoppingCartItem?> GetByCartIdAndProductIdAsync(Guid cartId, Guid productId)
    {
        return await _context.ShoppingCartItems
            .FirstOrDefaultAsync(i => i.ShoppingCartId == cartId && i.ProductId == productId);
    }

    public async Task<ShoppingCartItem> AddAsync(ShoppingCartItem item)
    {
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        _context.ShoppingCartItems.Add(item);
        await SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(ShoppingCartItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.ShoppingCartItems.Update(item);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid itemId)
    {
        var item = await GetByIdAsync(itemId);
        if (item != null)
        {
            _context.ShoppingCartItems.Remove(item);
            await SaveChangesAsync();
        }
    }

    public async Task DeleteByCartIdAsync(Guid cartId)
    {
        var items = await GetByCartIdAsync(cartId);
        _context.ShoppingCartItems.RemoveRange(items);
        await SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
