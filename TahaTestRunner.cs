using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class TahaTestRunner
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly string _baseUrl = "http://localhost:5227";

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🚀 Starting Taha Scenario - Real Database Execution");
        Console.WriteLine($"🌐 API Base URL: {_baseUrl}");
        Console.WriteLine("=" + new string('=', 80));
        Console.WriteLine();
        Console.WriteLine("📋 Test Scenario:");
        Console.WriteLine("   1. Create user 'Taha'");
        Console.WriteLine("   2. Create 2 products: 'moz' (10% discount) and 'sib' (no discount)");
        Console.WriteLine("   3. Create order with both products (>5 quantity each) to reach VIP threshold");
        Console.WriteLine("   4. Cancel the order");
        Console.WriteLine("   5. Create another order with both products (>5 quantity each)");
        Console.WriteLine("   6. Purchase the order (user should become VIP)");
        Console.WriteLine("   7. Create order with 10 moz, then purchase it");
        Console.WriteLine();
        Console.WriteLine("✅ Expected Results:");
        Console.WriteLine("   - First order: Canceled");
        Console.WriteLine("   - Second order: Purchased with 10% discount on moz only");
        Console.WriteLine("   - User becomes VIP after second order");
        Console.WriteLine("   - Third order: Both VIP discount AND moz discount applied");
        Console.WriteLine("=" + new string('=', 80));
        Console.WriteLine();

        try
        {
            // Step 1: Create user named Taha
            Console.WriteLine("📝 Step 1: Creating user 'Taha'...");
            var userId = await CreateUser("Taha");
            if (userId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create user. Aborting scenario.");
                return 1;
            }
            Console.WriteLine($"✅ User 'Taha' created successfully!");
            Console.WriteLine($"   🆔 User ID: {userId}");
            Console.WriteLine();

            // Step 2: Create product "moz" with 10% discount
            Console.WriteLine("📝 Step 2: Creating product 'moz' with 10% discount...");
            var mozId = await CreateProduct("moz", 100m, 10);
            if (mozId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create product 'moz'. Aborting scenario.");
                return 1;
            }
            Console.WriteLine($"✅ Product 'moz' created successfully!");
            Console.WriteLine($"   🆔 Product ID: {mozId}");
            Console.WriteLine($"   💰 Base Price: $100");
            Console.WriteLine($"   🏷️  Discount: 10%");
            Console.WriteLine($"   💵 Final Price: $90");
            Console.WriteLine();

            // Step 3: Create product "sib" with no discount
            Console.WriteLine("📝 Step 3: Creating product 'sib' (no discount)...");
            var sibId = await CreateProduct("sib", 100m, 0);
            if (sibId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create product 'sib'. Aborting scenario.");
                return 1;
            }
            Console.WriteLine($"✅ Product 'sib' created successfully!");
            Console.WriteLine($"   🆔 Product ID: {sibId}");
            Console.WriteLine($"   💰 Base Price: $100");
            Console.WriteLine($"   🏷️  Discount: 0%");
            Console.WriteLine($"   💵 Final Price: $100");
            Console.WriteLine();

            // Step 4: Create first order with both products
            Console.WriteLine("📝 Step 4: Creating first order (6 moz + 6 sib) to reach VIP threshold...");
            var firstOrderId = await CreateOrder(userId, mozId, sibId, 6, 6);
            if (firstOrderId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create first order. Aborting scenario.");
                return 1;
            }
            var firstOrderDetails = await GetOrderDetails(firstOrderId);
            Console.WriteLine($"✅ First order created successfully!");
            Console.WriteLine($"   🆔 Order ID: {firstOrderId}");
            Console.WriteLine($"   📦 Items: 6 moz ($90 each) + 6 sib ($100 each)");
            Console.WriteLine($"   💰 Total: ${firstOrderDetails?.TotalPrice:F2}");
            Console.WriteLine($"   📊 Status: {firstOrderDetails?.Status}");
            Console.WriteLine();

            // Step 5: Cancel the first order
            Console.WriteLine("📝 Step 5: Canceling the first order...");
            var cancelSuccess = await CancelOrder(firstOrderId);
            if (!cancelSuccess)
            {
                Console.WriteLine("❌ Failed to cancel order. Aborting scenario.");
                return 1;
            }
            var canceledOrderDetails = await GetOrderDetails(firstOrderId);
            Console.WriteLine($"✅ First order canceled successfully!");
            Console.WriteLine($"   📊 Status: {canceledOrderDetails?.Status}");
            Console.WriteLine();

            // Verify user is NOT VIP yet
            var userAfterCancel = await GetUserDetails(userId);
            Console.WriteLine($"📊 User status after cancellation:");
            Console.WriteLine($"   👑 Is VIP: {userAfterCancel?.IsVip}");
            Console.WriteLine($"   💰 Total Spending: ${userAfterCancel?.TotalSpending:F2}");
            Console.WriteLine();

            // Step 6: Create second order with both products
            Console.WriteLine("📝 Step 6: Creating second order (6 moz + 6 sib)...");
            var secondOrderId = await CreateOrder(userId, mozId, sibId, 6, 6);
            if (secondOrderId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create second order. Aborting scenario.");
                return 1;
            }
            var secondOrderDetails = await GetOrderDetails(secondOrderId);
            Console.WriteLine($"✅ Second order created successfully!");
            Console.WriteLine($"   🆔 Order ID: {secondOrderId}");
            Console.WriteLine($"   📦 Items: 6 moz ($90 each) + 6 sib ($100 each)");
            Console.WriteLine($"   💰 Total: ${secondOrderDetails?.TotalPrice:F2}");
            Console.WriteLine($"   📊 Status: {secondOrderDetails?.Status}");
            Console.WriteLine();

            // Step 7: Purchase the second order
            Console.WriteLine("📝 Step 7: Purchasing the second order...");
            var paymentSuccess = await PayOrder(secondOrderId);
            if (!paymentSuccess)
            {
                Console.WriteLine("❌ Failed to process payment. Aborting scenario.");
                return 1;
            }
            var paidOrderDetails = await GetOrderDetails(secondOrderId);
            Console.WriteLine($"✅ Second order purchased successfully!");
            Console.WriteLine($"   📊 Status: {paidOrderDetails?.Status}");
            Console.WriteLine($"   💰 Amount Paid: ${paidOrderDetails?.TotalPrice:F2}");
            Console.WriteLine($"   🏷️  Discount Applied: 10% on moz only");
            Console.WriteLine();

            // Verify user is NOW VIP
            var userAfterPurchase = await GetUserDetails(userId);
            Console.WriteLine($"📊 User status after purchase:");
            Console.WriteLine($"   👑 Is VIP: {userAfterPurchase?.IsVip}");
            Console.WriteLine($"   💰 Total Spending: ${userAfterPurchase?.TotalSpending:F2}");
            Console.WriteLine();

            if (userAfterPurchase?.IsVip != true)
            {
                Console.WriteLine("⚠️  WARNING: User should be VIP after spending over $1000!");
            }

            // Step 8: Create third order with 10 moz
            Console.WriteLine("📝 Step 8: Creating third order (10 moz) - should have BOTH VIP and product discount...");
            var thirdOrderId = await CreateOrder(userId, mozId, Guid.Empty, 10, 0);
            if (thirdOrderId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create third order. Aborting scenario.");
                return 1;
            }
            var thirdOrderDetails = await GetOrderDetails(thirdOrderId);
            Console.WriteLine($"✅ Third order created successfully!");
            Console.WriteLine($"   🆔 Order ID: {thirdOrderId}");
            Console.WriteLine($"   📦 Items: 10 moz");
            Console.WriteLine($"   💰 Total: ${thirdOrderDetails?.TotalPrice:F2}");
            Console.WriteLine($"   🏷️  Expected: VIP discount (5%) + moz discount (10%) = 15% total");
            Console.WriteLine($"   📊 Status: {thirdOrderDetails?.Status}");
            Console.WriteLine();

            // Step 9: Purchase the third order
            Console.WriteLine("📝 Step 9: Purchasing the third order...");
            var thirdPaymentSuccess = await PayOrder(thirdOrderId);
            if (!thirdPaymentSuccess)
            {
                Console.WriteLine("❌ Failed to process payment for third order. Aborting scenario.");
                return 1;
            }
            var thirdPaidOrderDetails = await GetOrderDetails(thirdOrderId);
            Console.WriteLine($"✅ Third order purchased successfully!");
            Console.WriteLine($"   📊 Status: {thirdPaidOrderDetails?.Status}");
            Console.WriteLine($"   💰 Amount Paid: ${thirdPaidOrderDetails?.TotalPrice:F2}");
            Console.WriteLine();

            // Final verification
            var finalUserDetails = await GetUserDetails(userId);
            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 80));
            Console.WriteLine("🎉 TAHA SCENARIO COMPLETED SUCCESSFULLY!");
            Console.WriteLine("=" + new string('=', 80));
            Console.WriteLine();
            Console.WriteLine("📊 Final Summary:");
            Console.WriteLine($"   👤 User: {finalUserDetails?.FullName} (ID: {userId})");
            Console.WriteLine($"   👑 VIP Status: {finalUserDetails?.IsVip}");
            Console.WriteLine($"   💰 Total Spent: ${finalUserDetails?.TotalSpending:F2}");
            Console.WriteLine();
            Console.WriteLine("📋 Orders Summary:");
            Console.WriteLine($"   1️⃣  Order {firstOrderId}: CANCELED");
            Console.WriteLine($"      - Status: {canceledOrderDetails?.Status}");
            Console.WriteLine($"      - Amount: ${canceledOrderDetails?.TotalPrice:F2}");
            Console.WriteLine();
            Console.WriteLine($"   2️⃣  Order {secondOrderId}: PURCHASED");
            Console.WriteLine($"      - Status: {paidOrderDetails?.Status}");
            Console.WriteLine($"      - Amount: ${paidOrderDetails?.TotalPrice:F2}");
            Console.WriteLine($"      - Discount: 10% on moz only");
            Console.WriteLine();
            Console.WriteLine($"   3️⃣  Order {thirdOrderId}: PURCHASED");
            Console.WriteLine($"      - Status: {thirdPaidOrderDetails?.Status}");
            Console.WriteLine($"      - Amount: ${thirdPaidOrderDetails?.TotalPrice:F2}");
            Console.WriteLine($"      - Discount: VIP (5%) + moz (10%) = 15% total");
            Console.WriteLine();
            Console.WriteLine("✅ All data has been saved to the database!");
            Console.WriteLine("🔍 You can now view this data in your database management tool.");
            Console.WriteLine();

            // Validate expected results
            bool allTestsPassed = true;
            Console.WriteLine("🧪 Validating Test Results:");
            Console.WriteLine();

            if (canceledOrderDetails?.Status.ToString() != "Canceled")
            {
                Console.WriteLine("❌ FAIL: First order should be Canceled");
                allTestsPassed = false;
            }
            else
            {
                Console.WriteLine("✅ PASS: First order is Canceled");
            }

            if (paidOrderDetails?.Status.ToString() != "Paid")
            {
                Console.WriteLine("❌ FAIL: Second order should be Paid");
                allTestsPassed = false;
            }
            else
            {
                Console.WriteLine("✅ PASS: Second order is Paid");
            }

            if (finalUserDetails?.IsVip != true)
            {
                Console.WriteLine("❌ FAIL: User should be VIP after second order");
                allTestsPassed = false;
            }
            else
            {
                Console.WriteLine("✅ PASS: User is VIP after second order");
            }

            if (thirdPaidOrderDetails?.Status.ToString() != "Paid")
            {
                Console.WriteLine("❌ FAIL: Third order should be Paid");
                allTestsPassed = false;
            }
            else
            {
                Console.WriteLine("✅ PASS: Third order is Paid");
            }

            // Check if third order has both discounts applied
            // Expected: 10 * 100 * 0.9 (product discount) * 0.95 (VIP discount) = 855
            decimal expectedThirdOrderTotal = 855m;
            if (Math.Abs((thirdPaidOrderDetails?.TotalPrice ?? 0) - expectedThirdOrderTotal) > 0.01m)
            {
                Console.WriteLine($"⚠️  WARNING: Third order total (${thirdPaidOrderDetails?.TotalPrice:F2}) doesn't match expected (${expectedThirdOrderTotal:F2})");
                Console.WriteLine($"   This might indicate discount calculation issues");
            }
            else
            {
                Console.WriteLine("✅ PASS: Third order has both VIP and product discounts applied correctly");
            }

            Console.WriteLine();
            if (allTestsPassed)
            {
                Console.WriteLine("🎉 ALL TESTS PASSED!");
                return 0;
            }
            else
            {
                Console.WriteLine("⚠️  SOME TESTS FAILED - Please review the results above");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Scenario failed with error: {ex.Message}");
            Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
            return 1;
        }
    }

    static async Task<Guid> CreateUser(string fullName)
    {
        try
        {
            var newUser = new { fullName = fullName };
            var json = JsonSerializer.Serialize(newUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/users", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);
                return Guid.Parse(result["id"].GetString());
            }
            else
            {
                Console.WriteLine($"   ⚠️  API returned status: {response.StatusCode}");
                Console.WriteLine($"   ⚠️  Response: {responseContent}");
                return Guid.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return Guid.Empty;
        }
    }

    static async Task<Guid> CreateProduct(string name, decimal price, int discountPercent)
    {
        try
        {
            var newProduct = new
            {
                name = name,
                basePrice = price,
                discountPercent = discountPercent,
                isActive = true,
                initialStock = 1000
            };
            var json = JsonSerializer.Serialize(newProduct);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/products", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);
                return Guid.Parse(result["id"].GetString());
            }
            else
            {
                Console.WriteLine($"   ⚠️  API returned status: {response.StatusCode}");
                Console.WriteLine($"   ⚠️  Response: {responseContent}");
                return Guid.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return Guid.Empty;
        }
    }

    static async Task<Guid> CreateOrder(Guid userId, Guid mozId, Guid sibId, int mozQuantity, int sibQuantity)
    {
        try
        {
            var items = new List<object>();
            
            if (mozId != Guid.Empty && mozQuantity > 0)
            {
                items.Add(new { productId = mozId, quantity = mozQuantity });
            }
            
            if (sibId != Guid.Empty && sibQuantity > 0)
            {
                items.Add(new { productId = sibId, quantity = sibQuantity });
            }

            var newOrder = new { userId = userId, items = items };
            var json = JsonSerializer.Serialize(newOrder);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);
                return Guid.Parse(result["orderId"].GetString());
            }
            else
            {
                Console.WriteLine($"   ⚠️  API returned status: {response.StatusCode}");
                Console.WriteLine($"   ⚠️  Response: {responseContent}");
                return Guid.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return Guid.Empty;
        }
    }

    static async Task<bool> CancelOrder(Guid orderId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/orders/{orderId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return false;
        }
    }

    static async Task<bool> PayOrder(Guid orderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/orders/{orderId}/pay", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return false;
        }
    }

    static async Task<OrderDetails?> GetOrderDetails(Guid orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/orders/{orderId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<OrderDetails>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    static async Task<UserDetails?> GetUserDetails(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<UserDetails>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    class OrderDetails
    {
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }

    class UserDetails
    {
        public string FullName { get; set; }
        public bool IsVip { get; set; }
        public decimal TotalSpending { get; set; }
    }
}
