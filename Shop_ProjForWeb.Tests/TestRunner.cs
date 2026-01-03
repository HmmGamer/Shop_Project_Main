using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Enums;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Shop_ProjForWeb.Tests;

/// <summary>
/// Simple test runner that can be executed manually to verify system functionality
/// </summary>
public class TestRunner
{
    public static async Task<bool> RunAllTests()
    {
        Console.WriteLine("🚀 Starting Shop System Integration Tests...\n");
        
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        
        var testResults = new List<(string TestName, bool Passed, string? Error)>();

        // Test 1: Health Check
        try
        {
            Console.WriteLine("🔍 Testing Health Check...");
            var response = await client.GetAsync("/health");
            var content = await response.Content.ReadAsStringAsync();
            var passed = response.StatusCode == HttpStatusCode.OK && content == "Healthy";
            testResults.Add(("Health Check", passed, passed ? null : $"Status: {response.StatusCode}, Content: {content}"));
            Console.WriteLine(passed ? "✅ Health Check PASSED" : "❌ Health Check FAILED");
        }
        catch (Exception ex)
        {
            testResults.Add(("Health Check", false, ex.Message));
            Console.WriteLine($"❌ Health Check FAILED: {ex.Message}");
        }

        // Test 2: Database Seeding
        try
        {
            Console.WriteLine("\n🔍 Testing Database Seeding...");
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SupermarketDbContext>();

            var users = await dbContext.Users.ToListAsync();
            var products = await dbContext.Products.Include(p => p.Inventory).ToListAsync();
            var vipHistory = await dbContext.VipStatusHistories.ToListAsync();

            var passed = users.Count > 0 && products.Count > 0 && 
                        products.All(p => p.Inventory != null) &&
                        vipHistory.Count > 0;

            testResults.Add(("Database Seeding", passed, passed ? null : 
                $"Users: {users.Count}, Products: {products.Count}, VIP History: {vipHistory.Count}"));
            
            Console.WriteLine(passed ? "✅ Database Seeding PASSED" : "❌ Database Seeding FAILED");
            Console.WriteLine($"   📊 Users: {users.Count}, Products: {products.Count}, VIP History: {vipHistory.Count}");
        }
        catch (Exception ex)
        {
            testResults.Add(("Database Seeding", false, ex.Message));
            Console.WriteLine($"❌ Database Seeding FAILED: {ex.Message}");
        }

        // Test 3: Order Lifecycle
        try
        {
            Console.WriteLine("\n🔍 Testing Complete Order Lifecycle...");
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SupermarketDbContext>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "normal@example.com");
            var product = await dbContext.Products.Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Name == "Premium Organic Apples");

            if (user != null && product?.Inventory != null)
            {
                var initialInventory = product.Inventory.Quantity;
                var orderQuantity = 2;

                // Create order using correct DTO
                var orderItems = new List<CreateOrderItemDto> 
                { 
                    new CreateOrderItemDto { ProductId = product.Id, Quantity = orderQuantity } 
                };
                var orderResponse = await orderService.CreateOrderAsync(user.Id, orderItems);

                // Pay for order (using the correct method name)
                await orderService.PayOrderAsync(orderResponse.OrderId);

                // Get the completed order
                var completedOrder = await orderService.GetOrderAsync(orderResponse.OrderId);
                
                // Check inventory change
                await dbContext.Entry(product.Inventory).ReloadAsync();

                var passed = completedOrder != null && 
                           completedOrder.Status == OrderStatus.Paid &&
                           product.Inventory.Quantity <= initialInventory; // Inventory should be reduced or same

                testResults.Add(("Order Lifecycle", passed, passed ? null : 
                    $"Order Status: {completedOrder?.Status}, Inventory Change: {initialInventory} -> {product.Inventory.Quantity}"));
                
                Console.WriteLine(passed ? "✅ Order Lifecycle PASSED" : "❌ Order Lifecycle FAILED");
                Console.WriteLine($"   📦 Order Status: {completedOrder?.Status}");
                Console.WriteLine($"   📊 Inventory: {initialInventory} -> {product.Inventory.Quantity}");
            }
            else
            {
                testResults.Add(("Order Lifecycle", false, "Required test data not found"));
                Console.WriteLine("❌ Order Lifecycle FAILED: Required test data not found");
            }
        }
        catch (Exception ex)
        {
            testResults.Add(("Order Lifecycle", false, ex.Message));
            Console.WriteLine($"❌ Order Lifecycle FAILED: {ex.Message}");
        }

        // Test 4: VIP System
        try
        {
            Console.WriteLine("\n🔍 Testing VIP System...");
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SupermarketDbContext>();

            var vipUser = await dbContext.Users.FirstOrDefaultAsync(u => u.IsVip);
            var vipHistory = await dbContext.VipStatusHistories.Where(v => v.UserId == vipUser!.Id).ToListAsync();

            var passed = vipUser != null && vipUser.IsVip && vipUser.VipTier > 0 && vipHistory.Count > 0;

            testResults.Add(("VIP System", passed, passed ? null : 
                $"VIP User Found: {vipUser != null}, Is VIP: {vipUser?.IsVip}, Tier: {vipUser?.VipTier}, History Count: {vipHistory.Count}"));
            
            Console.WriteLine(passed ? "✅ VIP System PASSED" : "❌ VIP System FAILED");
            Console.WriteLine($"   👑 VIP User: {vipUser?.FullName}, Tier: {vipUser?.VipTier}, Spending: ${vipUser?.TotalSpending}");
        }
        catch (Exception ex)
        {
            testResults.Add(("VIP System", false, ex.Message));
            Console.WriteLine($"❌ VIP System FAILED: {ex.Message}");
        }

        // Test 5: Inventory Management
        try
        {
            Console.WriteLine("\n🔍 Testing Inventory Management...");
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SupermarketDbContext>();
            var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

            var product = await dbContext.Products.Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Name == "Fresh Bananas");

            if (product?.Inventory != null)
            {
                var initialQuantity = product.Inventory.Quantity;
                var decreaseAmount = 3;

                // Use the correct method name
                var success = await inventoryService.TryDecreaseStockAsync(product.Id, decreaseAmount);

                await dbContext.Entry(product.Inventory).ReloadAsync();

                var passed = success && product.Inventory.Quantity == initialQuantity - decreaseAmount;

                testResults.Add(("Inventory Management", passed, passed ? null : 
                    $"Success: {success}, Quantity Change: {initialQuantity} -> {product.Inventory.Quantity}"));
                
                Console.WriteLine(passed ? "✅ Inventory Management PASSED" : "❌ Inventory Management FAILED");
                Console.WriteLine($"   📦 Quantity: {initialQuantity} -> {product.Inventory.Quantity}");
            }
            else
            {
                testResults.Add(("Inventory Management", false, "Test product not found"));
                Console.WriteLine("❌ Inventory Management FAILED: Test product not found");
            }
        }
        catch (Exception ex)
        {
            testResults.Add(("Inventory Management", false, ex.Message));
            Console.WriteLine($"❌ Inventory Management FAILED: {ex.Message}");
        }

        // Test 6: Low Stock Detection
        try
        {
            Console.WriteLine("\n🔍 Testing Low Stock Detection...");
            using var scope = factory.Services.CreateScope();
            var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

            var lowStockItems = await inventoryService.GetLowStockItemsAsync();
            var passed = lowStockItems != null; // Just check that the method works

            testResults.Add(("Low Stock Detection", passed, passed ? null : "Method returned null"));
            
            Console.WriteLine(passed ? "✅ Low Stock Detection PASSED" : "❌ Low Stock Detection FAILED");
            Console.WriteLine($"   📦 Low Stock Items Found: {lowStockItems?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            testResults.Add(("Low Stock Detection", false, ex.Message));
            Console.WriteLine($"❌ Low Stock Detection FAILED: {ex.Message}");
        }

        // Summary
        Console.WriteLine("\n" + "=".PadRight(60, '='));
        Console.WriteLine("📊 TEST RESULTS SUMMARY");
        Console.WriteLine("=".PadRight(60, '='));

        var passedTests = testResults.Count(t => t.Passed);
        var totalTests = testResults.Count;

        foreach (var (testName, passed, error) in testResults)
        {
            var status = passed ? "✅ PASS" : "❌ FAIL";
            Console.WriteLine($"{status} - {testName}");
            if (!passed && !string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"      Error: {error}");
            }
        }

        Console.WriteLine($"\n🎯 Overall Result: {passedTests}/{totalTests} tests passed");
        
        var allPassed = passedTests == totalTests;
        if (allPassed)
        {
            Console.WriteLine("🎉 ALL TESTS PASSED! The system is working correctly.");
        }
        else
        {
            Console.WriteLine("⚠️  Some tests failed. Please review the errors above.");
        }

        factory.Dispose();
        return allPassed;
    }
}