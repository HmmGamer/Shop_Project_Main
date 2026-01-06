namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Domain.Entities;

/// <summary>
/// Repository interface for shopping cart operations
/// </summary>
public interface IShoppingCartRepository
{
    /// <summary>
    /// Gets a cart by user ID
    /// </summary>
    Task<ShoppingCart?> GetByUserIdAsync(Guid userId);
    
    /// <summary>
    /// Gets a cart by ID with all items
    /// </summary>
    Task<ShoppingCart?> GetByIdAsync(Guid cartId);
    
    /// <summary>
    /// Gets a cart by ID with items included
    /// </summary>
    Task<ShoppingCart?> GetByIdWithItemsAsync(Guid cartId);
    
    /// <summary>
    /// Gets or creates a cart for a user
    /// </summary>
    Task<ShoppingCart> GetOrCreateCartAsync(Guid userId);
    
    /// <summary>
    /// Gets all active carts for a user
    /// </summary>
    Task<List<ShoppingCart>> GetActiveCartsByUserIdAsync(Guid userId);
    
    /// <summary>
    /// Gets abandoned carts older than specified date
    /// </summary>
    Task<List<ShoppingCart>> GetAbandonedCartsAsync(DateTime olderThan);
    
    /// <summary>
    /// Creates a new cart
    /// </summary>
    Task<ShoppingCart> CreateAsync(ShoppingCart cart);
    
    /// <summary>
    /// Updates a cart
    /// </summary>
    Task UpdateAsync(ShoppingCart cart);
    
    /// <summary>
    /// Deletes a cart and all its items
    /// </summary>
    Task DeleteAsync(Guid cartId);
    
    /// <summary>
    /// Saves changes
    /// </summary>
    Task SaveChangesAsync();
}

/// <summary>
/// Repository interface for shopping cart item operations
/// </summary>
public interface IShoppingCartItemRepository
{
    /// <summary>
    /// Gets an item by ID
    /// </summary>
    Task<ShoppingCartItem?> GetByIdAsync(Guid itemId);
    
    /// <summary>
    /// Gets all items in a cart
    /// </summary>
    Task<List<ShoppingCartItem>> GetByCartIdAsync(Guid cartId);
    
    /// <summary>
    /// Gets an item by cart ID and product ID
    /// </summary>
    Task<ShoppingCartItem?> GetByCartIdAndProductIdAsync(Guid cartId, Guid productId);
    
    /// <summary>
    /// Adds an item to cart
    /// </summary>
    Task<ShoppingCartItem> AddAsync(ShoppingCartItem item);
    
    /// <summary>
    /// Updates an item
    /// </summary>
    Task UpdateAsync(ShoppingCartItem item);
    
    /// <summary>
    /// Deletes an item
    /// </summary>
    Task DeleteAsync(Guid itemId);
    
    /// <summary>
    /// Deletes all items in a cart
    /// </summary>
    Task DeleteByCartIdAsync(Guid cartId);
    
    /// <summary>
    /// Saves changes
    /// </summary>
    Task SaveChangesAsync();
}
