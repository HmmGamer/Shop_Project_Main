namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Application.DTOs;

public interface IInventoryService
{
    Task DecreaseStockAsync(Guid productId, int quantity);
    Task<bool> TryDecreaseStockAsync(Guid productId, int quantity);
    Task<bool> ReserveStockAsync(Guid productId, int quantity);
    Task ReleaseStockAsync(Guid productId, int quantity);
    Task CommitReservationAsync(Guid productId, int quantity);
    Task IncreaseStockAsync(Guid productId, int quantity);
    Task<bool> CheckStockAvailabilityAsync(Guid productId, int quantity);
    Task<InventoryStatusDto> GetInventoryStatusAsync(Guid productId);
    Task<List<InventoryDto>> GetLowStockItemsAsync();
}
