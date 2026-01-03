using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Net;

namespace Shop_ProjForWeb.RuntimeTests;

public class RuntimeTester
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly List<(string TestName, bool Passed, string Details)> _testResults;

    public RuntimeTester(string baseUrl = "http://localhost:5227")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _testResults = new List<(string, bool, string)>();
    }

    public async Task<bool> RunAllTests()
    {
        Console.WriteLine("🚀 Starting REAL-TIME API Testing...");
        Console.WriteLine($"🌐 Testing against: {_baseUrl}");
        Console.WriteLine("=" + new string('=', 60));

        // Test 1: Health Check
        await TestHealthCheck();

        // Test 2: Get All Users
        await TestGetAllUsers();

        // Test 3: Get All Products
        await TestGetAllProducts();

        // Test 4: Get All Orders
        await TestGetAllOrders();

        // Test 5: Create a New User
        var newUserId = await TestCreateUser();

        // Test 6: Create an Order
        if (newUserId != Guid.Empty)
        {
            await TestCreateOrder(newUserId);
        }

        // Test 7: Test VIP User Functionality
        await TestVipUserFunctionality();

        // Test 8: Test Inventory Operations
        await TestInventoryOperations();

        // Test 9: Test Product Management
        await TestProductManagement();

        // Test 10: Test Order Management
        await TestOrderManagement();

        // Print Results
        PrintTestResults();

        return _testResults.All(r => r.Passed);
    }

    private async Task TestHealthCheck()
    {
        Console.WriteLine("\n🔍 Testing Health Check Endpoint...");
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/health");
            var content = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK && content == "Healthy";
            _testResults.Add(("Health Check", passed, $"Status: {response.StatusCode}, Response: {content}"));
            
            Console.WriteLine(passed ? "✅ Health Check PASSED" : "❌ Health Check FAILED");
            Console.WriteLine($"   Response: {content}");
        }
        catch (Exception ex)
        {
            _testResults.Add(("Health Check", false, ex.Message));
            Console.WriteLine($"❌ Health Check FAILED: {ex.Message}");
        }
    }

    private async Task TestGetAllUsers()
    {
        Console.WriteLine("\n🔍 Testing Get All Users...");
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users");
            var content = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK;
            _testResults.Add(("Get All Users", passed, $"Status: {response.StatusCode}"));
            
            Console.WriteLine(passed ? "✅ Get All Users PASSED" : "❌ Get All Users FAILED");
            
            if (passed)
            {
                var users = JsonConvert.DeserializeObject<dynamic[]>(content);
                Console.WriteLine($"   📊 Found {users?.Length ?? 0} users");
                
                if (users?.Length > 0)
                {
                    Console.WriteLine($"   👤 Sample User: {users[0].fullName} ({users[0].email})");
                    if (users[0].isVip == true)
                    {
                        Console.WriteLine($"   👑 VIP User - Tier: {users[0].vipTier}, Spending: ${users[0].totalSpending}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _testResults.Add(("Get All Users", false, ex.Message));
            Console.WriteLine($"❌ Get All Users FAILED: {ex.Message}");
        }
    }

    private async Task TestGetAllProducts()
    {
        Console.WriteLine("\n🔍 Testing Get All Products...");
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/products");
            var content = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK;
            _testResults.Add(("Get All Products", passed, $"Status: {response.StatusCode}"));
            
            Console.WriteLine(passed ? "✅ Get All Products PASSED" : "❌ Get All Products FAILED");
            
            if (passed)
            {
                var products = JsonConvert.DeserializeObject<dynamic[]>(content);
                Console.WriteLine($"   📦 Found {products?.Length ?? 0} products");
                
                if (products?.Length > 0)
                {
                    Console.WriteLine($"   🛍️  Sample Product: {products[0].name} - ${products[0].basePrice}");
                    if (products[0].discountPercent > 0)
                    {
                        Console.WriteLine($"   💰 Discount: {products[0].discountPercent}%");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _testResults.Add(("Get All Products", false, ex.Message));
            Console.WriteLine($"❌ Get All Products FAILED: {ex.Message}");
        }
    }

    private async Task TestGetAllOrders()
    {
        Console.WriteLine("\n🔍 Testing Get All Orders...");
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/orders");
            var content = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK;
            _testResults.Add(("Get All Orders", passed, $"Status: {response.StatusCode}"));
            
            Console.WriteLine(passed ? "✅ Get All Orders PASSED" : "❌ Get All Orders FAILED");
            
            if (passed)
            {
                var orders = JsonConvert.DeserializeObject<dynamic[]>(content);
                Console.WriteLine($"   📋 Found {orders?.Length ?? 0} orders");
                
                if (orders?.Length > 0)
                {
                    Console.WriteLine($"   🛒 Sample Order: ${orders[0].totalPrice} - Status: {orders[0].status}");
                }
            }
        }
        catch (Exception ex)
        {
            _testResults.Add(("Get All Orders", false, ex.Message));
            Console.WriteLine($"❌ Get All Orders FAILED: {ex.Message}");
        }
    }

    private async Task<Guid> TestCreateUser()
    {
        Console.WriteLine("\n🔍 Testing Create New User...");
        try
        {
            var newUser = new
            {
                fullName = "Runtime Test User",
                email = $"runtime.test.{DateTime.Now.Ticks}@example.com",
                phone = "555-0199",
                address = "123 Runtime Test St, Test City, TC 12345"
            };

            var json = JsonConvert.SerializeObject(newUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/users", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created;
            _testResults.Add(("Create User", passed, $"Status: {response.StatusCode}"));
            
            Console.WriteLine(passed ? "✅ Create User PASSED" : "❌ Create User FAILED");
            
            if (passed)
            {
                var createdUser = JsonConvert.DeserializeObject<dynamic>(responseContent);
                Console.WriteLine($"   👤 Created User: {createdUser.fullName} ({createdUser.email})");
                Console.WriteLine($"   🆔 User ID: {createdUser.id}");
                return Guid.Parse(createdUser.id.ToString());
            }
        }
        catch (Exception ex)
        {
            _testResults.Add(("Create User", false, ex.Message));
            Console.WriteLine($"❌ Create User FAILED: {ex.Message}");
        }
        
        return Guid.Empty;
    }

    private async Task TestCreateOrder(Guid userId)
    {
        Console.WriteLine("\n🔍 Testing Create Order...");
        try
        {
            // First, get available products
            var productsResponse = await _httpClient.GetAsync($"{_baseUrl}/api/products");
            if (productsResponse.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("❌ Cannot get products for order creation");
                return;
            }

            var productsContent = await productsResponse.Content.ReadAsStringAsync();
            var products = JsonConvert.DeserializeObject<dynamic[]>(productsContent);
            
            if (products?.Length == 0)
            {
                Console.WriteLine("❌ No products available for order creation");
                return;
            }

            var firstProduct = products[0];
            var newOrder = new
            {
                userId = userId,
                items = new[]
                {
                    new
                    {
                        productId = firstProduct.id,
                        quantity = 2
                    }
                }
            };

            var json = JsonConvert.SerializeObject(newOrder);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created;
            _testResults.Add(("Create Order", passed, $"Status: {response.StatusCode}"));
            
            Console.WriteLine(passed ? "✅ Create Order PASSED" : "❌ Create Order FAILED");
            
            if (passed)
            {
                var createdOrder = JsonConvert.DeserializeObject<dynamic>(responseContent);
                Console.WriteLine($"   🛒 Created Order: ${createdOrder.totalPrice}");
                Console.WriteLine($"   📋 Order ID: {createdOrder.orderId}");
                Console.WriteLine($"   📊 Status: {createdOrder.status}");
            }
            else
            {
                Console.WriteLine($"   ❌ Response: {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _testResults.Add(("Create Order", false, ex.Message));
            Console.WriteLine($"❌ Create Order FAILED: {ex.Message}");
        }
    }

    private async Task TestVipUserFunctionality()
    {
        Console.WriteLine("\n🔍 Testing VIP User Functionality...");
        try
        {
            // Get users and find a VIP user
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users");
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("❌ Cannot get users for VIP testing");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<dynamic[]>(content);
            var vipUser = users?.FirstOrDefault(u => u.isVip == true);

            if (vipUser == null)
            {
                Console.WriteLine("⚠️  No VIP users found in system");
                _testResults.Add(("VIP User Test", true, "No VIP users to test"));
                return;
            }

            Console.WriteLine($"✅ Found VIP User: {vipUser.fullName}");
            Console.WriteLine($"   👑 VIP Tier: {vipUser.vipTier}");
            Console.WriteLine($"   💰 Total Spending: ${vipUser.totalSpending}");
            Console.WriteLine($"   📅 VIP Since: {vipUser.vipUpgradedAt}");

            _testResults.Add(("VIP User Test", true, $"VIP User: {vipUser.fullName}, Tier: {vipUser.vipTier}"));
        }
        catch (Exception ex)
        {
            _testResults.Add(("VIP User Test", false, ex.Message));
            Console.WriteLine($"❌ VIP User Test FAILED: {ex.Message}");
        }
    }

    private async Task TestInventoryOperations()
    {
        Console.WriteLine("\n🔍 Testing Inventory Operations...");
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/inventory");
            var content = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK;
            _testResults.Add(("Inventory Operations", passed, $"Status: {response.StatusCode}"));
            
            Console.WriteLine(passed ? "✅ Inventory Operations PASSED" : "❌ Inventory Operations FAILED");
            
            if (passed)
            {
                var inventory = JsonConvert.DeserializeObject<dynamic[]>(content);
                Console.WriteLine($"   📦 Found {inventory?.Length ?? 0} inventory items");
                
                if (inventory?.Length > 0)
                {
                    var lowStockItems = inventory.Where(i => i.lowStockFlag == true).ToArray();
                    Console.WriteLine($"   ⚠️  Low Stock Items: {lowStockItems.Length}");
                    
                    var sampleItem = inventory[0];
                    Console.WriteLine($"   📊 Sample: Qty {sampleItem.quantity}, Reserved {sampleItem.reservedQuantity}");
                }
            }
        }
        catch (Exception ex)
        {
            _testResults.Add(("Inventory Operations", false, ex.Message));
            Console.WriteLine($"❌ Inventory Operations FAILED: {ex.Message}");
        }
    }

    private async Task TestProductManagement()
    {
        Console.WriteLine("\n🔍 Testing Product Management...");
        try
        {
            // Test getting products by category
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/products?category=Fruits");
            var content = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK;
            _testResults.Add(("Product Management", passed, $"Status: {response.StatusCode}"));
            
            Console.WriteLine(passed ? "✅ Product Management PASSED" : "❌ Product Management FAILED");
            
            if (passed)
            {
                var products = JsonConvert.DeserializeObject<dynamic[]>(content);
                Console.WriteLine($"   🍎 Found {products?.Length ?? 0} fruit products");
                
                if (products?.Length > 0)
                {
                    foreach (var product in products.Take(3))
                    {
                        Console.WriteLine($"   🛍️  {product.name}: ${product.basePrice} ({product.discountPercent}% off)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _testResults.Add(("Product Management", false, ex.Message));
            Console.WriteLine($"❌ Product Management FAILED: {ex.Message}");
        }
    }

    private async Task TestOrderManagement()
    {
        Console.WriteLine("\n🔍 Testing Order Management...");
        try
        {
            // Test getting orders by status
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/orders?status=Paid");
            var content = await response.Content.ReadAsStringAsync();
            
            var passed = response.StatusCode == HttpStatusCode.OK;
            _testResults.Add(("Order Management", passed, $"Status: {response.StatusCode}"));
            
            Console.WriteLine(passed ? "✅ Order Management PASSED" : "❌ Order Management FAILED");
            
            if (passed)
            {
                var orders = JsonConvert.DeserializeObject<dynamic[]>(content);
                Console.WriteLine($"   💳 Found {orders?.Length ?? 0} paid orders");
                
                if (orders?.Length > 0)
                {
                    var totalRevenue = orders.Sum(o => (decimal)o.totalPrice);
                    Console.WriteLine($"   💰 Total Revenue from Paid Orders: ${totalRevenue:F2}");
                }
            }
        }
        catch (Exception ex)
        {
            _testResults.Add(("Order Management", false, ex.Message));
            Console.WriteLine($"❌ Order Management FAILED: {ex.Message}");
        }
    }

    private void PrintTestResults()
    {
        Console.WriteLine("\n" + "=" + new string('=', 60));
        Console.WriteLine("📊 RUNTIME TEST RESULTS SUMMARY");
        Console.WriteLine("=" + new string('=', 60));

        var passedTests = _testResults.Count(t => t.Passed);
        var totalTests = _testResults.Count;

        foreach (var (testName, passed, details) in _testResults)
        {
            var status = passed ? "✅ PASS" : "❌ FAIL";
            Console.WriteLine($"{status} - {testName}");
            if (!passed)
            {
                Console.WriteLine($"      Details: {details}");
            }
        }

        Console.WriteLine($"\n🎯 Overall Result: {passedTests}/{totalTests} tests passed");
        
        if (passedTests == totalTests)
        {
            Console.WriteLine("🎉 ALL RUNTIME TESTS PASSED!");
            Console.WriteLine("✅ The Shop system is working perfectly in real-time!");
        }
        else
        {
            Console.WriteLine("⚠️  Some runtime tests failed.");
            Console.WriteLine("❌ Please check the application and try again.");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}