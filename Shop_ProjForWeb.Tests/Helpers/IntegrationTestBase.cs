namespace Shop_ProjForWeb.Tests.Helpers;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

public abstract class IntegrationTestBase : IDisposable
{
    protected HttpClient Client { get; }
    private WebApplicationFactory<Program> Factory { get; }
    private IServiceScope _scope;
    protected SupermarketDbContext DbContext { get; }
    private readonly string _databaseName;

    protected IntegrationTestBase()
    {
        _databaseName = $"TestDb_{Guid.NewGuid()}";
        
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<SupermarketDbContext>));
                    
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing with the same database name
                    services.AddDbContext<SupermarketDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(_databaseName);
                        options.EnableSensitiveDataLogging();
                        // Suppress transaction warnings for in-memory database
                        options.ConfigureWarnings(warnings =>
                            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    });
                });
            });

        Client = Factory.CreateClient();
        _scope = Factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<SupermarketDbContext>();
        
        // Ensure database is created
        DbContext.Database.EnsureCreated();
    }

    protected async Task SeedTestDataAsync()
    {
        // Override in derived classes to seed specific test data
        await Task.CompletedTask;
    }

    protected async Task CleanupTestDataAsync()
    {
        // Clear all data from database
        DbContext.Users.RemoveRange(DbContext.Users);
        DbContext.Products.RemoveRange(DbContext.Products);
        DbContext.Orders.RemoveRange(DbContext.Orders);
        DbContext.Inventories.RemoveRange(DbContext.Inventories);
        await DbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        _scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
