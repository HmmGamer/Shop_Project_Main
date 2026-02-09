using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;
using Microsoft.EntityFrameworkCore;
using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Core.Application.Services;

namespace Shop_ProjForWeb.Infrastructure.Persistent.Configurations
{
    public static class DbSeeder
    {
        // Image URL mapping for products
        private static readonly Dictionary<string, string> ProductImageUrls = new()
        {
        { "Organic Honeycrisp Apples", "https://picsum.photos/seed/apples/400/400" },
        { "Fresh Bananas", "https://picsum.photos/seed/bananas/400/400" },
        { "Whole Milk Gallon", "https://picsum.photos/seed/milk/400/400" },
        { "Artisan Sourdough Bread", "https://picsum.photos/seed/bread/400/400" },
        { "Free-Range Chicken Breast", "https://picsum.photos/seed/chicken/400/400" },
        { "Atlantic Salmon Fillet", "https://picsum.photos/seed/salmon/400/400" },
        { "Organic Baby Spinach", "https://picsum.photos/seed/spinach/400/400" },
        { "Roma Tomatoes", "https://picsum.photos/seed/tomatoes/400/400" },
        { "Sharp Cheddar Cheese", "https://picsum.photos/seed/cheese/400/400" },
        { "Greek Yogurt", "https://picsum.photos/seed/yogurt/400/400" },
        { "Chocolate Croissants", "https://picsum.photos/seed/croissant/400/400" },
        { "Ground Beef 80/20", "https://picsum.photos/seed/beef/400/400" },
        { "Fresh Strawberries", "https://picsum.photos/seed/strawberries/400/400" },
        { "Avocados", "https://picsum.photos/seed/avocado/400/400" },
        { "Pasta Penne", "https://picsum.photos/seed/pasta/400/400" },
        { "Extra Virgin Olive Oil", "https://picsum.photos/seed/oliveoil/400/400" },
        { "Organic Eggs", "https://picsum.photos/seed/eggs/400/400" },
        { "Vanilla Ice Cream", "https://picsum.photos/seed/icecream/400/400" },
        { "Frozen Blueberries", "https://picsum.photos/seed/blueberries/400/400" },
        { "Quinoa", "https://picsum.photos/seed/quinoa/400/400" },
        { "Almond Butter", "https://picsum.photos/seed/almondbutter/400/400" },
        { "Orange Juice", "https://picsum.photos/seed/orangejuice/400/400" },
        { "Green Tea", "https://picsum.photos/seed/greentea/400/400" },
        { "Dark Chocolate Bar", "https://picsum.photos/seed/darkchocolate/400/400" },
        { "Mixed Nuts", "https://picsum.photos/seed/mixednuts/400/400" }
        };

        public static async Task SeedAsync(SupermarketDbContext db)
        {
            // Check if we need to update existing products with images
            var existingProducts = await db.Products.ToListAsync();
            if (existingProducts.Any())
            {
                var updated = false;
                foreach (var product in existingProducts)
                {
                    if (string.IsNullOrEmpty(product.ImageUrl))
                    {
                        if (ProductImageUrls.TryGetValue(product.Name, out var imageUrl))
                        {
                            product.ImageUrl = imageUrl;
                            updated = true;
                        }
                        else
                        {
                            // Default image for unknown products
                            product.ImageUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?w=400";
                            updated = true;
                        }
                    }
                }
                if (updated)
                {
                    await db.SaveChangesAsync();
                }
                return;
            }

            var random = new Random(42); // Fixed seed for consistent data

            // Default password for seeded users (development only)
            const string defaultPassword = "1234!";

            // Seed users with hardcoded data
            // Note: IsVip is now a computed property (VipTier > 0), not stored in DB
            var users = new List<User>
            {
                new User { FullName = "John Hooper", Email = "john.hooper@email.com", Phone = "555-0101", Address = "123 Oak Street, Springfield, IL 62701", TotalSpending = 245.50m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Sarah Mitchell", Email = "sarah.mitchell@email.com", Phone = "555-0102", Address = "456 Maple Avenue, Chicago, IL 60601", TotalSpending = 1850.75m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-45), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Michael Rodriguez", Email = "michael.rodriguez@email.com", Phone = "555-0103", Address = "789 Pine Road, Austin, TX 78701", TotalSpending = 567.25m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Emily Johnson", Email = "emily.johnson@email.com", Phone = "555-0104", Address = "321 Elm Drive, Seattle, WA 98101", TotalSpending = 2340.00m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-120), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "David Thompson", Email = "david.thompson@email.com", Phone = "555-0105", Address = "654 Cedar Lane, Denver, CO 80201", TotalSpending = 892.30m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Jessica Williams", Email = "jessica.williams@email.com", Phone = "555-0106", Address = "987 Birch Street, Miami, FL 33101", TotalSpending = 1675.50m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-30), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Christopher Brown", Email = "christopher.brown@email.com", Phone = "555-0107", Address = "147 Willow Way, Portland, OR 97201", TotalSpending = 423.75m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Amanda Davis", Email = "amanda.davis@email.com", Phone = "555-0108", Address = "258 Spruce Circle, Boston, MA 02101", TotalSpending = 756.90m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Robert Wilson", Email = "robert.wilson@email.com", Phone = "555-0109", Address = "369 Ash Boulevard, Phoenix, AZ 85001", TotalSpending = 3120.25m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-90), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Lisa Anderson", Email = "lisa.anderson@email.com", Phone = "555-0110", Address = "741 Poplar Place, Nashville, TN 37201", TotalSpending = 634.40m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "James Taylor", Email = "james.taylor@email.com", Phone = "555-0111", Address = "852 Hickory Hill, Atlanta, GA 30301", TotalSpending = 2890.80m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-75), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Michelle Garcia", Email = "michelle.garcia@email.com", Phone = "555-0112", Address = "963 Magnolia Manor, San Diego, CA 92101", TotalSpending = 445.60m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Kevin Martinez", Email = "kevin.martinez@email.com", Phone = "555-0113", Address = "159 Dogwood Drive, Las Vegas, NV 89101", TotalSpending = 789.15m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Rachel Lee", Email = "rachel.lee@email.com", Phone = "555-0114", Address = "357 Sycamore Street, Minneapolis, MN 55401", TotalSpending = 1945.30m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-60), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Daniel White", Email = "daniel.white@email.com", Phone = "555-0115", Address = "468 Chestnut Court, Detroit, MI 48201", TotalSpending = 523.85m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Nicole Harris", Email = "nicole.harris@email.com", Phone = "555-0116", Address = "579 Walnut Walk, Philadelphia, PA 19101", TotalSpending = 678.20m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Matthew Clark", Email = "matthew.clark@email.com", Phone = "555-0117", Address = "680 Beech Bay, San Francisco, CA 94101", TotalSpending = 2567.45m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-105), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Stephanie Lewis", Email = "stephanie.lewis@email.com", Phone = "555-0118", Address = "791 Redwood Ridge, Houston, TX 77001", TotalSpending = 834.70m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Andrew Robinson", Email = "andrew.robinson@email.com", Phone = "555-0119", Address = "802 Cypress Cove, New York, NY 10001", TotalSpending = 3456.90m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-150), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Jennifer Walker", Email = "jennifer.walker@email.com", Phone = "555-0120", Address = "913 Fir Forest, Los Angeles, CA 90001", TotalSpending = 456.35m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Ryan Hall", Email = "ryan.hall@email.com", Phone = "555-0121", Address = "124 Juniper Junction, Orlando, FL 32801", TotalSpending = 712.50m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Kimberly Allen", Email = "kimberly.allen@email.com", Phone = "555-0122", Address = "235 Laurel Lane, Salt Lake City, UT 84101", TotalSpending = 1789.25m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-25), PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Brandon Young", Email = "brandon.young@email.com", Phone = "555-0123", Address = "346 Hemlock Heights, Kansas City, MO 64101", TotalSpending = 598.80m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Megan King", Email = "megan.king@email.com", Phone = "555-0124", Address = "457 Sequoia Square, Charlotte, NC 28201", TotalSpending = 923.40m, VipTier = 0, PasswordHash = PasswordHasher.HashPassword(defaultPassword) },
                new User { FullName = "Tyler Wright", Email = "tyler.wright@email.com", Phone = "555-0125", Address = "568 Cottonwood Circle, Columbus, OH 43201", TotalSpending = 2123.65m, VipTier = 1, VipUpgradedAt = DateTime.UtcNow.AddDays(-80), PasswordHash = PasswordHasher.HashPassword(defaultPassword) }
            };

            await db.Users.AddRangeAsync(users);
            await db.SaveChangesAsync();

            // Create 25 realistic products with hardcoded data
            var products = new List<Product>
            {
                new Product { Name = "Organic Honeycrisp Apples", Description = "Sweet and crispy organic apples, perfect for snacking", Category = "Fruits", BasePrice = 5.99m, DiscountPercent = 10, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400" },
                new Product { Name = "Fresh Bananas", Description = "Ripe yellow bananas, great source of potassium", Category = "Fruits", BasePrice = 2.49m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400" },
                new Product { Name = "Whole Milk Gallon", Description = "Fresh whole milk from local dairy farms", Category = "Dairy", BasePrice = 4.29m, DiscountPercent = 5, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1563636619-e9143da7973b?w=400" },
                new Product { Name = "Artisan Sourdough Bread", Description = "Handcrafted sourdough bread with crispy crust", Category = "Bakery", BasePrice = 6.99m, DiscountPercent = 15, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400" },
                new Product { Name = "Free-Range Chicken Breast", Description = "Premium free-range chicken breast, boneless", Category = "Meat", BasePrice = 12.99m, DiscountPercent = 8, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1604503468506-a8da13d82791?w=400" },
                new Product { Name = "Atlantic Salmon Fillet", Description = "Fresh Atlantic salmon, wild-caught", Category = "Seafood", BasePrice = 18.99m, DiscountPercent = 12, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1499125562588-29fb8a56b5d5?w=400" },
                new Product { Name = "Organic Baby Spinach", Description = "Fresh organic baby spinach leaves", Category = "Vegetables", BasePrice = 3.99m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400" },
                new Product { Name = "Roma Tomatoes", Description = "Fresh Roma tomatoes, perfect for cooking", Category = "Vegetables", BasePrice = 2.99m, DiscountPercent = 5, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1546470427-227c7369a9b8?w=400" },
                new Product { Name = "Sharp Cheddar Cheese", Description = "Aged sharp cheddar cheese block", Category = "Dairy", BasePrice = 7.49m, DiscountPercent = 10, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1618164436241-4473940d1f5c?w=400" },
                new Product { Name = "Greek Yogurt", Description = "Creamy Greek yogurt, high in protein", Category = "Dairy", BasePrice = 5.99m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400" },
                new Product { Name = "Chocolate Croissants", Description = "Buttery croissants filled with chocolate", Category = "Bakery", BasePrice = 8.99m, DiscountPercent = 20, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=400" },
                new Product { Name = "Ground Beef 80/20", Description = "Fresh ground beef, 80% lean", Category = "Meat", BasePrice = 9.99m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=400" },
                new Product { Name = "Fresh Strawberries", Description = "Sweet and juicy strawberries", Category = "Fruits", BasePrice = 4.99m, DiscountPercent = 15, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?w=400" },
                new Product { Name = "Avocados", Description = "Ripe Hass avocados, perfect for guacamole", Category = "Fruits", BasePrice = 1.99m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?w=400" },
                new Product { Name = "Pasta Penne", Description = "Italian durum wheat penne pasta", Category = "Pantry", BasePrice = 2.99m, DiscountPercent = 5, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1551462147-ff29053bfc14?w=400" },
                new Product { Name = "Extra Virgin Olive Oil", Description = "Cold-pressed extra virgin olive oil", Category = "Pantry", BasePrice = 12.99m, DiscountPercent = 8, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400" },
                new Product { Name = "Organic Eggs", Description = "Free-range organic eggs, dozen", Category = "Dairy", BasePrice = 6.99m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400" },
                new Product { Name = "Vanilla Ice Cream", Description = "Premium vanilla ice cream, half gallon", Category = "Frozen", BasePrice = 7.99m, DiscountPercent = 12, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1570197788417-0e82375c9371?w=400" },
                new Product { Name = "Frozen Blueberries", Description = "Wild frozen blueberries, antioxidant rich", Category = "Frozen", BasePrice = 5.49m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1498557850523-fd3d118b962e?w=400" },
                new Product { Name = "Quinoa", Description = "Organic quinoa, superfood grain", Category = "Pantry", BasePrice = 8.99m, DiscountPercent = 10, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400" },
                new Product { Name = "Almond Butter", Description = "Natural almond butter, no added sugar", Category = "Pantry", BasePrice = 11.99m, DiscountPercent = 5, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1612187209234-a0e7e5c0e1c8?w=400" },
                new Product { Name = "Orange Juice", Description = "Fresh squeezed orange juice, no pulp", Category = "Beverages", BasePrice = 4.99m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=400" },
                new Product { Name = "Green Tea", Description = "Premium green tea bags, 20 count", Category = "Beverages", BasePrice = 6.49m, DiscountPercent = 15, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1556881286-fc6915169721?w=400" },
                new Product { Name = "Dark Chocolate Bar", Description = "70% cocoa dark chocolate bar", Category = "Snacks", BasePrice = 3.99m, DiscountPercent = 0, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1548907040-4baa42d10919?w=400" },
                new Product { Name = "Mixed Nuts", Description = "Premium mixed nuts, lightly salted", Category = "Snacks", BasePrice = 9.99m, DiscountPercent = 8, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1536816579748-4ecb3f03d72a?w=400" }
            };

            await db.Products.AddRangeAsync(products);
            await db.SaveChangesAsync();

            // Create inventory for each product (after products are saved and have IDs)
            var inventories = new List<Inventory>();
            var savedProducts = await db.Products.ToListAsync();
            foreach (var product in savedProducts)
            {
                var quantity = random.Next(10, 200);
                var threshold = random.Next(5, 25);
                inventories.Add(new Inventory
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    ReservedQuantity = random.Next(0, Math.Min(10, quantity)),
                    LowStockThreshold = threshold,
                    LowStockFlag = quantity < threshold,
                    LastUpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                });
            }
            await db.Inventories.AddRangeAsync(inventories);
            await db.SaveChangesAsync();

            // Create some realistic orders with order items
            var orders = new List<Order>();
            var orderItems = new List<OrderItem>();
            var savedUsers = await db.Users.ToListAsync();
            
            // Create 15 orders with various statuses
            for (int i = 0; i < 15; i++)
            {
                var user = savedUsers[random.Next(savedUsers.Count)];
                var orderProducts = savedProducts.OrderBy(x => random.Next()).Take(random.Next(1, 5)).ToList();
                
                var order = new Order
                {
                    UserId = user.Id,
                    Status = (OrderStatus)(i % 5), // Cycle through statuses
                    PaymentStatus = i % 3 == 0 ? PaymentStatus.Success : PaymentStatus.Pending,
                    TotalPrice = 0, // Will calculate after adding items
                    PaidAt = i % 3 == 0 ? DateTime.UtcNow.AddDays(-random.Next(1, 30)) : null,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 60))
                };
                
                decimal orderTotal = 0;
                foreach (var product in orderProducts)
                {
                    var quantity = random.Next(1, 5);
                    var unitPrice = product.BasePrice * (1 - product.DiscountPercent / 100m);
                    var vipDiscount = user.VipTier > 0 ? 5 : 0; // Use VipTier instead of IsVip
                    
                    orderTotal += unitPrice * quantity;
                    
                    orderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        UnitPrice = unitPrice,
                        Quantity = quantity,
                        ProductDiscountPercent = product.DiscountPercent,
                        VipDiscountPercent = vipDiscount,
                        CreatedAt = order.CreatedAt
                    });
                }
                
                order.TotalPrice = orderTotal;
                orders.Add(order);
            }
            
            await db.Orders.AddRangeAsync(orders);
            await db.SaveChangesAsync();
            await db.OrderItems.AddRangeAsync(orderItems);
            await db.SaveChangesAsync();

            // Create VIP status history for VIP users
            var vipHistories = new List<VipStatusHistory>();
            var vipUsers = savedUsers.Where(u => u.VipTier > 0).ToList(); // Use VipTier instead of IsVip
            
            foreach (var vipUser in vipUsers)
            {
                vipHistories.Add(new VipStatusHistory
                {
                    UserId = vipUser.Id,
                    PreviousTier = 0,
                    NewTier = 1,
                    TriggeringOrderTotal = random.Next(100, 500),
                    TotalSpendingAtUpgrade = vipUser.TotalSpending,
                    Reason = "Upgraded to VIP tier 1 after reaching spending threshold",
                    CreatedAt = vipUser.VipUpgradedAt ?? DateTime.UtcNow.AddDays(-30)
                });
            }
            
            await db.VipStatusHistories.AddRangeAsync(vipHistories);
            await db.SaveChangesAsync();
        }
    }
}
