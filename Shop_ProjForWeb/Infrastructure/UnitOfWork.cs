using Microsoft.EntityFrameworkCore.Storage;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;
using Shop_ProjForWeb.Infrastructure.Repositories;

namespace Shop_ProjForWeb.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly SupermarketDbContext _context;
    private IDbContextTransaction? _transaction;
    
    private IProductRepository? _products;
    private IInventoryRepository? _inventory;
    private IOrderRepository? _orders;
    private IOrderItemRepository? _orderItems;
    private IUserRepository? _users;

    public UnitOfWork(SupermarketDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products => 
        _products ??= new ProductRepository(_context);

    public IInventoryRepository Inventory => 
        _inventory ??= new InventoryRepository(_context);

    public IOrderRepository Orders => 
        _orders ??= new OrderRepository(_context);

    public IOrderItemRepository OrderItems => 
        _orderItems ??= new OrderItemRepository(_context);

    public IUserRepository Users => 
        _users ??= new UserRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }
        
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to commit");
        }

        try
        {
            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
        }
        catch
        {
            await _transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to rollback");
        }

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        if (_transaction != null)
        {
            // Already in transaction, just execute
            return await operation();
        }

        await BeginTransactionAsync();
        try
        {
            var result = await operation();
            await CommitTransactionAsync();
            return result;
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        if (_transaction != null)
        {
            // Already in transaction, just execute
            await operation();
            return;
        }

        await BeginTransactionAsync();
        try
        {
            await operation();
            await CommitTransactionAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}