using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Core.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Shop_ProjForWeb.Core.Application.Services;

public class ShoppingCartService : IShoppingCartService
{
    private readonly IShoppingCartRepository _cartRepository;
    private readonly IShoppingCartItemRepository _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IVipStatusCalculator _vipStatusCalculator;
    private readonly ILogger<ShoppingCartService> _logger;

    public ShoppingCartService(
        IShoppingCartRepository cartRepository,
        IShoppingCartItemRepository cartItemRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IVipStatusCalculator vipStatusCalculator,
        ILogger<ShoppingCartService> logger)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _vipStatusCalculator = vipStatusCalculator;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await _cartRepository.GetOrCreateCartAsync(userId);
        await _cartRepository.SaveChangesAsync();
        
        return MapToCartDto(cart);
    }

    public async Task<CartSummaryDto> GetCartSummaryAsync(Guid userId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        
        if (cart == null || !cart.Items.Any())
        {
            return new CartSummaryDto();
        }

        return new CartSummaryDto
        {
            TotalItems = cart.TotalItems,
            TotalPrice = cart.TotalPrice,
            ItemsCount = cart.TotalItems,
            Items = cart.Items.Select(i => new CartItemSummaryDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice,
                ProductImageUrl = i.ProductImageUrl
            }).ToList()
        };
    }

    public async Task<CartItemDto> AddItemAsync(Guid userId, AddCartItemDto dto)
    {
        // Get or create cart
        var cart = await _cartRepository.GetOrCreateCartAsync(userId);
        
        // Get product
        var product = await _productRepository.GetByIdAsync(dto.ProductId)
            ?? throw new ProductNotFoundException($"Product with ID {dto.ProductId} not found");
        
        // Check if product is active
        if (!product.IsActive)
        {
            throw new InvalidOperationException($"Product {product.Name} is not available");
        }
        
        // Check if item already exists in cart
        var existingItem = await _cartItemRepository.GetByCartIdAndProductIdAsync(cart.Id, dto.ProductId);
        
        int newQuantity = dto.Quantity;
        decimal unitPrice = product.BasePrice;
        int discountPercent = product.DiscountPercent;
        
        if (existingItem != null)
        {
            // Update existing item
            newQuantity = existingItem.Quantity + dto.Quantity;
            existingItem.Quantity = newQuantity;
            existingItem.Notes = dto.Notes;
            existingItem.CalculateTotals();
            
            await _cartItemRepository.UpdateAsync(existingItem);
            _logger.LogInformation("Updated cart item {ItemId} for user {UserId}", existingItem.Id, userId);
            
            // Update cart totals
            cart.RecalculateTotals();
            await _cartRepository.UpdateAsync(cart);
            
            return MapToCartItemDto(existingItem);
        }
        
        // Create new item
        var item = new ShoppingCartItem
        {
            ShoppingCartId = cart.Id,
            ProductId = dto.ProductId,
            ProductName = product.Name,
            ProductImageUrl = product.ImageUrl,
            Quantity = dto.Quantity,
            OriginalUnitPrice = unitPrice,
            ProductDiscountPercent = discountPercent,
            Notes = dto.Notes
        };
        
        item.CalculateTotals();
        
        var newItem = await _cartItemRepository.AddAsync(item);
        
        // Update cart totals
        cart.RecalculateTotals();
        await _cartRepository.UpdateAsync(cart);
        
        _logger.LogInformation("Added new item {ItemId} to cart {CartId} for user {UserId}", 
            newItem.Id, cart.Id, userId);
        
        return MapToCartItemDto(newItem);
    }

    public async Task<CartItemDto> UpdateItemQuantityAsync(Guid userId, UpdateCartItemDto dto)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Cart not found");
        
        var item = await _cartItemRepository.GetByIdAsync(dto.ItemId)
            ?? throw new InvalidOperationException("Cart item not found");
        
        if (item.ShoppingCartId != cart.Id)
        {
            throw new InvalidOperationException("Item does not belong to this cart");
        }
        
        if (dto.Quantity <= 0)
        {
            await RemoveItemAsync(userId, dto.ItemId);
            return null!;
        }
        
        // Validate stock
        var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId);
        if (inventory != null && dto.Quantity > inventory.Quantity)
        {
            throw new InvalidOperationException($"Only {inventory.Quantity} items available in stock");
        }
        
        item.Quantity = dto.Quantity;
        item.CalculateTotals();
        
        await _cartItemRepository.UpdateAsync(item);
        
        // Update cart totals
        cart.RecalculateTotals();
        await _cartRepository.UpdateAsync(cart);
        
        _logger.LogInformation("Updated item {ItemId} quantity to {Quantity}", item.Id, dto.Quantity);
        
        return MapToCartItemDto(item);
    }

    public async Task<bool> RemoveItemAsync(Guid userId, Guid itemId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart == null)
        {
            return false;
        }
        
        var item = await _cartItemRepository.GetByIdAsync(itemId);
        if (item == null || item.ShoppingCartId != cart.Id)
        {
            return false;
        }
        
        await _cartItemRepository.DeleteAsync(itemId);
        
        // Update cart totals
        cart.RecalculateTotals();
        await _cartRepository.UpdateAsync(cart);
        
        _logger.LogInformation("Removed item {ItemId} from cart {CartId}", itemId, cart.Id);
        
        return true;
    }

    public async Task<bool> ClearCartAsync(Guid userId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart == null)
        {
            return false;
        }
        
        await _cartItemRepository.DeleteByCartIdAsync(cart.Id);
        
        cart.TotalItems = 0;
        cart.Subtotal = 0;
        cart.DiscountAmount = 0;
        cart.TotalPrice = 0;
        
        await _cartRepository.UpdateAsync(cart);
        
        _logger.LogInformation("Cleared cart {CartId} for user {UserId}", cart.Id, userId);
        
        return true;
    }

    public async Task<CartDto> MergeCartsAsync(Guid sourceUserId, Guid targetUserId)
    {
        var sourceCart = await _cartRepository.GetByUserIdAsync(sourceUserId);
        var targetCart = await _cartRepository.GetOrCreateCartAsync(targetUserId);
        
        if (sourceCart == null || !sourceCart.Items.Any())
        {
            return MapToCartDto(targetCart);
        }
        
        foreach (var sourceItem in sourceCart.Items.ToList())
        {
            // Check if product exists and is active
            var product = await _productRepository.GetByIdAsync(sourceItem.ProductId);
            if (product == null || !product.IsActive)
            {
                continue;
            }
            
            // Check if item already exists in target cart
            var targetItem = await _cartItemRepository.GetByCartIdAndProductIdAsync(targetCart.Id, sourceItem.ProductId);
            
            if (targetItem != null)
            {
                // Merge quantities
                targetItem.Quantity = Math.Min(targetItem.Quantity + sourceItem.Quantity, 1000);
                targetItem.CalculateTotals();
                await _cartItemRepository.UpdateAsync(targetItem);
            }
            else
            {
                // Add new item
                var newItem = new ShoppingCartItem
                {
                    ShoppingCartId = targetCart.Id,
                    ProductId = sourceItem.ProductId,
                    ProductName = sourceItem.ProductName,
                    ProductImageUrl = sourceItem.ProductImageUrl,
                    Quantity = sourceItem.Quantity,
                    OriginalUnitPrice = sourceItem.OriginalUnitPrice,
                    ProductDiscountPercent = sourceItem.ProductDiscountPercent,
                    ProductDiscountAmount = sourceItem.ProductDiscountAmount,
                    FinalUnitPrice = sourceItem.FinalUnitPrice,
                    TotalPrice = sourceItem.TotalPrice
                };
                
                await _cartItemRepository.AddAsync(newItem);
            }
            
            // Remove from source
            await _cartItemRepository.DeleteAsync(sourceItem.Id);
        }
        
        // Update target cart totals
        targetCart.RecalculateTotals();
        await _cartRepository.UpdateAsync(targetCart);
        
        // Mark source cart as abandoned
        sourceCart.Status = CartStatus.Abandoned;
        await _cartRepository.UpdateAsync(sourceCart);
        
        _logger.LogInformation("Merged cart from user {SourceUserId} to user {TargetUserId}", 
            sourceUserId, targetUserId);
        
        return MapToCartDto(targetCart);
    }

    public async Task<CartDto> ApplyVipDiscountAsync(Guid userId, int vipDiscountPercent)
    {
        var cart = await _cartRepository.GetOrCreateCartAsync(userId);
        
        cart.VipDiscountPercent = vipDiscountPercent;
        cart.RecalculateTotals();
        
        await _cartRepository.UpdateAsync(cart);
        
        _logger.LogInformation("Applied VIP discount {Percent}% to cart {CartId}", vipDiscountPercent, cart.Id);
        
        return MapToCartDto(cart);
    }

    public async Task<CartValidationResult> ValidateCartAsync(Guid userId)
    {
        var result = new CartValidationResult { IsValid = true };
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        
        if (cart == null || !cart.Items.Any())
        {
            return result;
        }
        
        foreach (var item in cart.Items)
        {
            // Check if product still exists and is active
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null || !product.IsActive)
            {
                result.IsValid = false;
                result.Errors.Add(new CartValidationError
                {
                    ItemId = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ErrorType = "ProductUnavailable",
                    Message = $"Product {item.ProductName} is no longer available"
                });
                continue;
            }
            
            // Check price changes
            if (product.BasePrice != item.OriginalUnitPrice)
            {
                result.Warnings.Add(new CartValidationWarning
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    WarningType = "PriceChanged",
                    Message = $"Price has changed from {item.OriginalUnitPrice} to {product.BasePrice}"
                });
            }
            
            // Check stock
            var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId);
            if (inventory != null && inventory.Quantity < item.Quantity)
            {
                result.Warnings.Add(new CartValidationWarning
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    WarningType = "LowStock",
                    Message = $"Only {inventory.Quantity} items available in stock"
                });
            }
        }
        
        return result;
    }

    public async Task<List<OrderItemDto>> ConvertToOrderItemsAsync(Guid userId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        
        if (cart == null || !cart.Items.Any())
        {
            throw new InvalidOperationException("Cart is empty");
        }
        
        var validation = await ValidateCartAsync(userId);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Cart validation failed: {string.Join(", ", validation.Errors.Select(e => e.Message))}");
        }
        
        var orderItems = cart.Items.Select(item => new OrderItemDto
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = item.FinalUnitPrice,
            ProductDiscountPercent = item.ProductDiscountPercent,
            ProductDiscountAmount = item.ProductDiscountAmount,
            VipDiscountPercent = 0,
            TotalPrice = item.TotalPrice
        }).ToList();
        
        // Mark cart as converted
        cart.Status = CartStatus.ConvertedToOrder;
        await _cartRepository.UpdateAsync(cart);
        
        _logger.LogInformation("Converted cart {CartId} to order items for user {UserId}", cart.Id, userId);
        
        return orderItems;
    }

    public async Task<bool> DeleteCartAsync(Guid cartId)
    {
        var cart = await _cartRepository.GetByIdAsync(cartId);
        if (cart == null)
        {
            return false;
        }
        
        await _cartItemRepository.DeleteByCartIdAsync(cartId);
        await _cartRepository.DeleteAsync(cartId);
        
        return true;
    }

    #region Mapping Methods

    private CartDto MapToCartDto(ShoppingCart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Status = cart.Status.ToString(),
            TotalItems = cart.TotalItems,
            Subtotal = cart.Subtotal,
            DiscountAmount = cart.DiscountAmount,
            TotalPrice = cart.TotalPrice,
            VipDiscountPercent = cart.VipDiscountPercent,
            Items = cart.Items.Select(MapToCartItemDto).ToList(),
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt
        };
    }

    private CartItemDto MapToCartItemDto(ShoppingCartItem item)
    {
        return new CartItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ProductImageUrl = item.ProductImageUrl,
            Quantity = item.Quantity,
            OriginalUnitPrice = item.OriginalUnitPrice,
            ProductDiscountPercent = item.ProductDiscountPercent,
            ProductDiscountAmount = item.ProductDiscountAmount,
            FinalUnitPrice = item.FinalUnitPrice,
            TotalPrice = item.TotalPrice,
            Notes = item.Notes,
            AddedAt = item.CreatedAt
        };
    }

    #endregion
}
