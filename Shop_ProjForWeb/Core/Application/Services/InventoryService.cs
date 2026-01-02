namespace Shop_ProjForWeb.Core.Application.Services;

using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Exceptions;

public class InventoryService(IInventoryRepository inventoryRepository) : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;

    public async Task DecreaseStockAsync(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero");
        }

        var success = await TryDecreaseStockAsync(productId, quantity);
        if (!success)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
            if (inventory == null)
            {
                throw new ProductNotFoundException($"Inventory not found for product {productId}");
            }
            throw new InsufficientStockException($"Insufficient stock. Available: {inventory.Quantity}, Requested: {quantity}");
        }
    }

    public async Task<bool> TryDecreaseStockAsync(Guid productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");

        const int maxRetries = 5; // Increased retries for better concurrency handling
        var random = new Random();
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
                if (inventory == null)
                    return false;

                if (inventory.Quantity < quantity)
                    return false;

                // Use the entity's DecreaseStock method which includes validation
                inventory.DecreaseStock(quantity);
                await _inventoryRepository.UpdateAsync(inventory);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == maxRetries - 1) 
                    throw;
                
                // Exponential backoff with jitter to reduce thundering herd
                var baseDelay = 100 * (int)Math.Pow(2, attempt);
                var jitter = random.Next(0, baseDelay / 2);
                await Task.Delay(baseDelay + jitter);
            }
            catch (InvalidOperationException)
            {
                // Stock validation failed, return false
                return false;
            }
        }
        return false;
    }

    public async Task<bool> ReserveStockAsync(Guid productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");

        const int maxRetries = 5; // Increased retries for better concurrency handling
        var random = new Random();
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
                if (inventory == null)
                    return false;

                if (!inventory.CanReserve(quantity))
                    return false;

                inventory.ReserveStock(quantity);
                await _inventoryRepository.UpdateAsync(inventory);

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == maxRetries - 1)
                    throw;
                
                // Exponential backoff with jitter
                var baseDelay = 100 * (int)Math.Pow(2, attempt);
                var jitter = random.Next(0, baseDelay / 2);
                await Task.Delay(baseDelay + jitter);
            }
            catch (InvalidOperationException)
            {
                // Reservation validation failed, return false
                return false;
            }
        }
        return false;
    }

    public async Task ReleaseStockAsync(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero");
        }

        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
                if (inventory == null)
                {
                    throw new ProductNotFoundException($"Inventory not found for product {productId}");
                }

                // Check if we have reservations to release, otherwise just increase stock
                if (inventory.ReservedQuantity >= quantity)
                {
                    inventory.ReleaseReservation(quantity);
                }
                else
                {
                    // No reservation exists (stock was already committed), just increase stock
                    inventory.IncreaseStock(quantity);
                }
                await _inventoryRepository.UpdateAsync(inventory);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == maxRetries - 1)
                    throw;
                
                await Task.Delay(100 * (attempt + 1));
            }
        }
    }

    public async Task CommitReservationAsync(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero");
        }

        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
                if (inventory == null)
                {
                    throw new ProductNotFoundException($"Inventory not found for product {productId}");
                }

                inventory.CommitReservation(quantity);
                await _inventoryRepository.UpdateAsync(inventory);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == maxRetries - 1)
                    throw;
                
                await Task.Delay(100 * (attempt + 1));
            }
        }
    }

    public async Task IncreaseStockAsync(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero");
        }

        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(productId);

                if (inventory == null)
                {
                    throw new ProductNotFoundException($"Inventory not found for product {productId}");
                }

                inventory.IncreaseStock(quantity);
                await _inventoryRepository.UpdateAsync(inventory);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == maxRetries - 1)
                    throw;
                
                // Exponential backoff
                await Task.Delay(100 * (attempt + 1));
            }
        }
    }

    public async Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantity)
    {
        return await _inventoryRepository.CheckStockAvailabilityAsync(productId, quantity);
    }

    public async Task<InventoryStatusDto> GetInventoryStatusAsync(Guid productId)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
        if (inventory == null)
        {
            throw new ProductNotFoundException($"Inventory not found for product {productId}");
        }

        return new InventoryStatusDto
        {
            ProductId = productId,
            ProductName = inventory.Product?.Name ?? "Unknown",
            StockQuantity = inventory.AvailableQuantity,
            IsLowStock = inventory.LowStockFlag,
            ReorderLevel = inventory.LowStockThreshold
        };
    }

    public async Task<List<InventoryDto>> GetLowStockItemsAsync()
    {
        var items = await _inventoryRepository.GetLowStockItemsAsync();
        return items.Select(i => new InventoryDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "Unknown",
            Quantity = i.AvailableQuantity,
            LowStockFlag = i.LowStockFlag,
            LastUpdatedAt = i.LastUpdatedAt
        }).ToList();
    }
}
