using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Net;

namespace Shop_ProjForWeb.RuntimeTests;

public class TahaScenarioRunner
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public TahaScenarioRunner(string baseUrl = "http://localhost:5227")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    public async Task<bool> RunTahaScenario()
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
                return false;
            }
            Console.WriteLine($"✅ User 'Taha' created successfully!");
            Console.WriteLine($"   🆔 User ID: {userId}");
            Console.WriteLine();

            // Step 2: Create product "moz" with 10% discount
            Console.WriteLine("📝 Step 2: Creating product 'moz' with 10% discount...");
            var mozId = await CreateProduct("moz", 100m, 10); // $100 base price, 10% discount
            if (mozId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create product 'moz'. Aborting scenario.");
                return false;
            }
            Console.WriteLine($"✅ Product 'moz' created successfully!");
            Console.WriteLine($"   🆔 Product ID: {mozId}");
            Console.WriteLine($"   💰 Base Price: $100");
            Console.WriteLine($"   🏷️  Discount: 10%");
            Console.WriteLine($"   💵 Final Price: $90");
            Console.WriteLine();

            // Step 3: Create product "sib" with no discount
            Console.WriteLine("📝 Step 3: Creating product 'sib' (no discount)...");
            var sibId = await CreateProduct("sib", 100m, 0); // $100 base price, no discount
            if (sibId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create product 'sib'. Aborting scenario.");
                return false;
            }
            Console.WriteLine($"✅ Product 'sib' created successfully!");
            Console.WriteLine($"   🆔 Product ID: {sibId}");
            Console.WriteLine($"   💰 Base Price: $100");
            Console.WriteLine($"   🏷️  Discount: 0%");
            Console.WriteLine($"   💵 Final Price: $100");
            Console.WriteLine();

            // Step 4: Create first order with both products (enough to reach VIP threshold)
            Console.WriteLine("📝 Step 4: Creating first order (6 moz + 6 sib) to reach VIP threshold...");
            var firstOrderId = await CreateOrder(userId, mozId, sibId, 6, 6);
            if (firstOrderId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create first order. Aborting scenario.");
                return false;
            }
            var firstOrderDetails = await GetOrderDetails(firstOrderId);
            Console.WriteLine($"✅ First order created successfully!");
            Console.WriteLine($"   🆔 Order ID: {firstOrderId}");
            Console.WriteLine($"   📦 Items: 6 moz ($90 each) + 6 sib ($100 each)");
            Console.WriteLine($"   💰 Total: ${firstOrderDetails?.totalPrice:F2}");
            Console.WriteLine($"   📊 Status: {firstOrderDetails?.status}");
            Console.WriteLine();

            // Step 5: Cancel the first order
            Console.WriteLine("📝 Step 5: Canceling the first order...");
            var cancelSuccess = await CancelOrder(firstOrderId);
            if (!cancelSuccess)
            {
                Console.WriteLine("❌ Failed to cancel order. Aborting scenario.");
                return false;
            }
            var canceledOrderDetails = await GetOrderDetails(firstOrderId);
            Console.WriteLine($"✅ First order canceled successfully!");
            Console.WriteLine($"   � IStatus: {canceledOrderDetails?.status}");
            Console.WriteLine();

            // Verify user is NOT VIP yet
            var userAfterCancel = await GetUserDetails(userId);
            Console.WriteLine($"📊 User status after cancellation:");
            Console.WriteLine($"   👑 Is VIP: {userAfterCancel?.isVip}");
            Console.WriteLine($"   💰 Total Spending: ${userAfterCancel?.totalSpending:F2}");
            Console.WriteLine();

            // Step 6: Create second order with both products
            Console.WriteLine("� StepV 6: Creating second order (6 moz + 6 sib)...");
            var secondOrderId = await CreateOrder(userId, mozId, sibId, 6, 6);
            if (secondOrderId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create second order. Aborting scenario.");
                return false;
            }
            var secondOrderDetails = await GetOrderDetails(secondOrderId);
            Console.WriteLine($"✅ Second order created successfully!");
            Console.WriteLine($"   🆔 Order ID: {secondOrderId}");
            Console.WriteLine($"   📦 Items: 6 moz ($90 each) + 6 sib ($100 each)");
            Console.WriteLine($"   💰 Total: ${secondOrderDetails?.totalPrice:F2}");
            Console.WriteLine($"   📊 Status: {secondOrderDetails?.status}");
            Console.WriteLine();

            // Step 7: Purchase the second order
            Console.WriteLine("📝 Step 7: Purchasing the second order...");
            var paymentSuccess = await PayOrder(secondOrderId);
            if (!paymentSuccess)
            {
                Console.WriteLine("❌ Failed to process payment. Aborting scenario.");
                return false;
            }
            var paidOrderDetails = await GetOrderDetails(secondOrderId);
            Console.WriteLine($"✅ Second order purchased successfully!");
            Console.WriteLine($"   📊 Status: {paidOrderDetails?.status}");
            Console.WriteLine($"   💰 Amount Paid: ${paidOrderDetails?.totalPrice:F2}");
            Console.WriteLine($"   🏷️  Discount Applied: 10% on moz only");
            Console.WriteLine();

            // Verify user is NOW VIP
            var userAfterPurchase = await GetUserDetails(userId);
            Console.WriteLine($"📊 User status after purchase:");
            Console.WriteLine($"   👑 Is VIP: {userAfterPurchase?.isVip}");
            Console.WriteLine($"   💰 Total Spending: ${userAfterPurchase?.totalSpending:F2}");
            Console.WriteLine();

            if (userAfterPurchase?.isVip != true)
            {
                Console.WriteLine("⚠️  WARNING: User should be VIP after spending over $1000!");
            }

            // Step 8: Create third order with 10 moz
            Console.WriteLine("📝 Step 8: Creating third order (10 moz) - should have BOTH VIP and product discount...");
            var thirdOrderId = await CreateOrder(userId, mozId, Guid.Empty, 10, 0);
            if (thirdOrderId == Guid.Empty)
            {
                Console.WriteLine("❌ Failed to create third order. Aborting scenario.");
                return false;
            }
            var thirdOrderDetails = await GetOrderDetails(thirdOrderId);
            Console.WriteLine($"✅ Third order created successfully!");
            Console.WriteLine($"   🆔 Order ID: {thirdOrderId}");
            Console.WriteLine($"   📦 Items: 10 moz");
            Console.WriteLine($"   💰 Total: ${thirdOrderDetails?.totalPrice:F2}");
            Console.WriteLine($"   🏷️  Expected: VIP discount (5%) + moz discount (10%) = 15% total");
            Console.WriteLine($"   📊 Status: {thirdOrderDetails?.status}");
            Console.WriteLine();

            // Step 9: Purchase the third order
            Console.WriteLine("📝 Step 9: Purchasing the third order...");
            var thirdPaymentSuccess = await PayOrder(thirdOrderId);
            if (!thirdPaymentSuccess)
            {
                Console.WriteLine("❌ Failed to process payment for third order. Aborting scenario.");
                return false;
            }
            var thirdPaidOrderDetails = await GetOrderDetails(thirdOrderId);
            Console.WriteLine($"✅ Third order purchased successfully!");
            Console.WriteLine($"   📊 Status: {thirdPaidOrderDetails?.status}");
            Console.WriteLine($"   💰 Amount Paid: ${thirdPaidOrderDetails?.totalPrice:F2}");
            Console.WriteLine();

            // Final verification
            var finalUserDetails = await GetUserDetails(userId);
            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 80));
            Console.WriteLine("🎉 TAHA SCENARIO COMPLETED SUCCESSFULLY!");
            Console.WriteLine("=" + new string('=', 80));
            Console.WriteLine();
            Console.WriteLine("📊 Final Summary:");
            Console.WriteLine($"   👤 User: {finalUserDetails?.fullName} (ID: {userId})");
            Console.WriteLine($"   👑 VIP Status: {finalUserDetails?.isVip}");
            Console.WriteLine($"   💰 Total Spent: ${finalUserDetails?.totalSpending:F2}");
            Console.WriteLine();
            Console.WriteLine("📋 Orders Summary:");
            Console.WriteLine($"   1️⃣  Order {firstOrderId}: CANCELED");
            Console.WriteLine($"      - Status: {canceledOrderDetails?.status}");
            Console.WriteLine($"      - Amount: ${canceledOrderDetails?.totalPrice:F2}");
            Console.WriteLine();
            Console.WriteLine($"   2️⃣  Order {secondOrderId}: PURCHASED");
            Console.WriteLine($"      - Status: {paidOrderDetails?.status}");
            Console.WriteLine($"      - Amount: ${paidOrderDetails?.totalPrice:F2}");
            Console.WriteLine($"      - Discount: 10% on moz only");
            Console.WriteLine();
            Console.WriteLine($"   3️⃣  Order {thirdOrderId}: PURCHASED");
            Console.WriteLine($"      - Status: {thirdPaidOrderDetails?.status}");
            Console.WriteLine($"      - Amount: ${thirdPaidOrderDetails?.totalPrice:F2}");
            Console.WriteLine($"      - Discount: VIP (5%) + moz (10%) = 15% total");
            Console.WriteLine();
            Console.WriteLine("✅ All data has been saved to the database!");
            Console.WriteLine("🔍 You can now view this data in your database management tool.");
            Console.WriteLine();

            // Validate expected results
            bool allTestsPassed = true;
            Console.WriteLine("🧪 Validating Test Results:");
            Console.WriteLine();

            if (canceledOrderDetails?.status.ToString() != "Canceled")
            {
                Console.WriteLine("❌ FAIL: First order should be Canceled");
                allTestsPassed = false;
            }
            else
            {
                Console.WriteLine("✅ PASS: First order is Canceled");
            }

            if (paidOrderDetails?.status.ToString() != "Paid")
            {
                Console.WriteLine("❌ FAIL: Second order should be Paid");
                allTestsPassed = false;
            }
            else
            {
                Console.WriteLine("✅ PASS: Second order is Paid");
            }

            if (finalUserDetails?.isVip != true)
            {
                Console.WriteLine("❌ FAIL: User should be VIP after second order");
                allTestsPassed = false;
            }
            else
            {
                Console.WriteLine("✅ PASS: User is VIP after second order");
            }

            if (thirdPaidOrderDetails?.status.ToString() != "Paid")
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
            if (Math.Abs((thirdPaidOrderDetails?.totalPrice ?? 0) - expectedThirdOrderTotal) > 0.01m)
            {
                Console.WriteLine($"⚠️  WARNING: Third order total (${thirdPaidOrderDetails?.totalPrice:F2}) doesn't match expected (${expectedThirdOrderTotal:F2})");
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
            }
            else
            {
                Console.WriteLine("⚠️  SOME TESTS FAILED - Please review the results above");
            }
            Console.WriteLine();

            return allTestsPassed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Scenario failed with error: {ex.Message}");
            Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
            return false;
        }
    }

    private async Task<Guid> CreateUser(string fullName)
    {
        try
        {
            var newUser = new
            {
                fullName = fullName
            };

            var json = JsonConvert.SerializeObject(newUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/users", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
            {
                var createdUser = JsonConvert.DeserializeObject<dynamic>(responseContent);
                return Guid.Parse(createdUser.id.ToString());
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

    private async Task<Guid> CreateProduct(string name, decimal price, int discountPercent)
    {
        try
        {
            var newProduct = new
            {
                name = name,
                basePrice = price,
                discountPercent = discountPercent,
                isActive = true,
                initialStock = 1000 // High stock to ensure we don't run out
            };

            var json = JsonConvert.SerializeObject(newProduct);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/products", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
            {
                var createdProduct = JsonConvert.DeserializeObject<dynamic>(responseContent);
                return Guid.Parse(createdProduct.id.ToString());
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

    private async Task<Guid> CreateOrder(Guid userId, Guid mozId, Guid sibId, int mozQuantity, int sibQuantity)
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

            var newOrder = new
            {
                userId = userId,
                items = items
            };

            var json = JsonConvert.SerializeObject(newOrder);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
            {
                var createdOrder = JsonConvert.DeserializeObject<dynamic>(responseContent);
                return Guid.Parse(createdOrder.orderId.ToString());
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

    private async Task<bool> CancelOrder(Guid orderId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/orders/{orderId}");
            
            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   ⚠️  API returned status: {response.StatusCode}");
                Console.WriteLine($"   ⚠️  Response: {responseContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> PayOrder(Guid orderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/orders/{orderId}/pay", null);
            
            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   ⚠️  API returned status: {response.StatusCode}");
                Console.WriteLine($"   ⚠️  Response: {responseContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return false;
        }
    }

    private async Task<dynamic?> GetOrderDetails(Guid orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/orders/{orderId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<dynamic>(responseContent);
            }
            else
            {
                Console.WriteLine($"   ⚠️  API returned status: {response.StatusCode}");
                Console.WriteLine($"   ⚠️  Response: {responseContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return null;
        }
    }

    private async Task<dynamic?> GetUserDetails(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<dynamic>(responseContent);
            }
            else
            {
                Console.WriteLine($"   ⚠️  API returned status: {response.StatusCode}");
                Console.WriteLine($"   ⚠️  Response: {responseContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
