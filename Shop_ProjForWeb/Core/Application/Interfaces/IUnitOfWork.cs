namespace Shop_ProjForWeb.Core.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IInventoryRepository Inventory { get; }
    IOrderRepository Orders { get; }
    IOrderItemRepository OrderItems { get; }
    IUserRepository Users { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    Task ExecuteInTransactionAsync(Func<Task> operation);
}