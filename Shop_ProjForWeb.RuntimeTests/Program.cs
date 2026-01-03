using Shop_ProjForWeb.RuntimeTests;

Console.WriteLine("🧪 Shop System REAL-TIME Testing Suite");
Console.WriteLine("======================================");
Console.WriteLine();
Console.WriteLine("This will test your running Shop application like a real user!");
Console.WriteLine("Make sure your Shop_ProjForWeb application is running first.");
Console.WriteLine();

Console.Write("Is your Shop application running on http://localhost:5227? (y/n): ");
var response = Console.ReadLine();

if (response?.ToLower() != "y")
{
    Console.WriteLine();
    Console.WriteLine("❌ Please start your Shop application first:");
    Console.WriteLine("   1. Open a terminal in the Shop_ProjForWeb directory");
    Console.WriteLine("   2. Run: dotnet run");
    Console.WriteLine("   3. Wait for 'Application started' message");
    Console.WriteLine("   4. Then run this test again");
    Console.WriteLine();
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    return;
}

Console.WriteLine();
Console.WriteLine("Select test to run:");
Console.WriteLine("1. Run all runtime tests");
Console.WriteLine("2. Run Taha scenario (creates real data in database)");
Console.Write("Enter choice (1 or 2): ");
var choice = Console.ReadLine();

Console.WriteLine();
Console.WriteLine("🚀 Starting real-time API testing...");
Console.WriteLine();

bool allTestsPassed = false;

try
{
    if (choice == "2")
    {
        // Run Taha scenario
        var tahaRunner = new TahaScenarioRunner();
        allTestsPassed = await tahaRunner.RunTahaScenario();
        tahaRunner.Dispose();
    }
    else
    {
        // Run all tests (default)
        var tester = new RuntimeTester();
        allTestsPassed = await tester.RunAllTests();
        tester.Dispose();
    }
    
    Console.WriteLine();
    Console.WriteLine("=" + new string('=', 60));
    
    if (allTestsPassed)
    {
        Console.WriteLine("🎉 SUCCESS: All operations completed successfully!");
        Console.WriteLine("✅ Your Shop system is working perfectly!");
        if (choice == "2")
        {
            Console.WriteLine("💾 Data has been saved to the database!");
        }
        else
        {
            Console.WriteLine("🚀 Ready for production use!");
        }
    }
    else
    {
        Console.WriteLine("⚠️  Some operations failed.");
        Console.WriteLine("❌ Please check the results above and fix any issues.");
    }
    
    Console.WriteLine("=" + new string('=', 60));
}
catch (Exception ex)
{
    Console.WriteLine($"💥 CRITICAL ERROR: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Possible causes:");
    Console.WriteLine("- Shop application is not running");
    Console.WriteLine("- Wrong URL (should be http://localhost:5227)");
    Console.WriteLine("- Network connectivity issues");
    Console.WriteLine("- Application startup errors");
}

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();