namespace Shop_ProjForWeb.Infrastructure.Persistent.DbContext;

using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Domain.Entities;
using System.Linq.Expressions;

public class SupermarketDbContext(DbContextOptions<SupermarketDbContext> options) : DbContext(options)
{
    public required DbSet<User> Users { get; set; }
    public required DbSet<Product> Products { get; set; }
    public required DbSet<Inventory> Inventories { get; set; }
    public required DbSet<Order> Orders { get; set; }
    public required DbSet<OrderItem> OrderItems { get; set; }
    public required DbSet<VipStatusHistory> VipStatusHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.Email).IsRequired(false);
            entity.Property(e => e.Phone).IsRequired(false);
            entity.Property(e => e.Address).IsRequired(false);
            // IsVip is now a computed property [NotMapped], not stored in DB
            entity.Property(e => e.TotalSpending).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.VipUpgradedAt).IsRequired(false);
            entity.Property(e => e.VipTier).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).IsRequired(false);
            
            // One-to-Many: User -> Orders
            entity.HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: User -> VipStatusHistory
            entity.HasMany(u => u.VipHistory)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Product Configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Description).IsRequired(false);
            entity.Property(e => e.Category).IsRequired(false);
            entity.Property(e => e.BasePrice).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.DiscountPercent).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).IsRequired(false);
            
            // One-to-One: Product -> Inventory (Required relationship)
            entity.HasOne(p => p.Inventory)
                .WithOne(i => i.Product)
                .HasForeignKey<Inventory>(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            
            // One-to-Many: Product -> OrderItems
            entity.HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict to prevent data loss
        });

        // Inventory Configuration
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.ReservedQuantity).IsRequired();
            entity.Property(e => e.LowStockThreshold).IsRequired();
            entity.Property(e => e.LowStockFlag).IsRequired();
            entity.Property(e => e.LastUpdatedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).IsRequired(false);
            
            // Unique constraint on ProductId to enforce one-to-one relationship
            entity.HasIndex(e => e.ProductId).IsUnique();
            
            // Foreign key constraint with explicit configuration
            entity.HasOne(i => i.Product)
                .WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // Order Configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.PaymentStatus).IsRequired();
            entity.Property(e => e.TotalPrice).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.PaidAt).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).IsRequired(false);
            
            // One-to-Many: Order -> OrderItems
            entity.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index on UserId for filtering user orders
            entity.HasIndex(e => e.UserId);
        });

        // OrderItem Configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.UnitPrice).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.ProductDiscountPercent).IsRequired();
            entity.Property(e => e.VipDiscountPercent).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).IsRequired(false);
        });

        // VipStatusHistory Configuration
        modelBuilder.Entity<VipStatusHistory>(entity =>
        {
            entity.ToTable("VipStatusHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PreviousTier).IsRequired();
            entity.Property(e => e.NewTier).IsRequired();
            entity.Property(e => e.TriggeringOrderTotal).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.TotalSpendingAtUpgrade).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).IsRequired(false);
            
            // Index for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

