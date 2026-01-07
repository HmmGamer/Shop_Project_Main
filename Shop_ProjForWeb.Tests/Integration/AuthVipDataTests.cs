using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;
using Shop_ProjForWeb.Core.Application.Services;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Tests.Helpers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Shop_ProjForWeb.Tests.Integration;

/// <summary>
/// Integration tests for Auth API VIP data exposure
/// Feature: vip-discount-display
/// </summary>
public class AuthVipDataTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Feature: vip-discount-display, Property 6: Login Response VIP Data
    /// For any successful login, the response SHALL contain both vipTier (integer 0-3) 
    /// and totalSpending (non-negative decimal) fields.
    /// Validates: Requirements 5.1, 5.2
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1500)]
    [InlineData(2, 10000)]
    [InlineData(3, 50000)]
    public async Task Login_ReturnsVipTierAndTotalSpending(int expectedTier, decimal totalSpending)
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var hashedPassword = PasswordHasher.HashPassword(password);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = email,
            PasswordHash = hashedPassword,
            VipTier = expectedTier,
            TotalSpending = totalSpending,
            VipUpgradedAt = expectedTier > 0 ? DateTime.UtcNow : null
        };
        
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/token", new { email, password });
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        
        var data = root.GetProperty("data");
        data.TryGetProperty("token", out _).Should().BeTrue("Response should contain token");
        data.TryGetProperty("user", out var userElement).Should().BeTrue("Response should contain user object");
        
        // Verify VIP data is present
        userElement.TryGetProperty("vipTier", out var vipTierElement).Should().BeTrue("User should have vipTier field");
        userElement.TryGetProperty("totalSpending", out var totalSpendingElement).Should().BeTrue("User should have totalSpending field");
        userElement.TryGetProperty("isVip", out var isVipElement).Should().BeTrue("User should have isVip field");
        
        // Verify VIP data values
        vipTierElement.GetInt32().Should().Be(expectedTier);
        totalSpendingElement.GetDecimal().Should().Be(totalSpending);
        isVipElement.GetBoolean().Should().Be(expectedTier > 0);
    }

    /// <summary>
    /// Feature: vip-discount-display, Property 6: Login Response VIP Data
    /// Validates that vipTier is always between 0 and 3.
    /// Validates: Requirements 5.1
    /// </summary>
    [Fact]
    public async Task Login_VipTier_IsWithinValidRange()
    {
        // Arrange - Create users with each valid tier
        var password = "TestPassword123!";
        var hashedPassword = PasswordHasher.HashPassword(password);
        
        for (int tier = 0; tier <= 3; tier++)
        {
            var email = $"tier{tier}_{Guid.NewGuid()}@example.com";
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = $"Tier {tier} User",
                Email = email,
                PasswordHash = hashedPassword,
                VipTier = tier,
                TotalSpending = tier * 10000m,
                VipUpgradedAt = tier > 0 ? DateTime.UtcNow : null
            };
            
            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/token", new { email, password });
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var vipTier = jsonDoc.RootElement
                .GetProperty("data")
                .GetProperty("user")
                .GetProperty("vipTier")
                .GetInt32();
            
            vipTier.Should().BeInRange(0, 3, $"VIP tier should be between 0 and 3, got {vipTier}");
        }
    }

    /// <summary>
    /// Feature: vip-discount-display, Property 6: Login Response VIP Data
    /// Validates that totalSpending is non-negative.
    /// Validates: Requirements 5.2
    /// </summary>
    [Fact]
    public async Task Login_TotalSpending_IsNonNegative()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var hashedPassword = PasswordHasher.HashPassword(password);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = email,
            PasswordHash = hashedPassword,
            VipTier = 0,
            TotalSpending = 0m
        };
        
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/token", new { email, password });
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var totalSpending = jsonDoc.RootElement
            .GetProperty("data")
            .GetProperty("user")
            .GetProperty("totalSpending")
            .GetDecimal();
        
        totalSpending.Should().BeGreaterOrEqualTo(0, "Total spending should never be negative");
    }

    /// <summary>
    /// Feature: vip-discount-display
    /// Validates that user object contains all required fields.
    /// Validates: Requirements 5.3
    /// </summary>
    [Fact]
    public async Task Login_UserObject_ContainsAllRequiredFields()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var hashedPassword = PasswordHasher.HashPassword(password);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User Full Name",
            Email = email,
            PasswordHash = hashedPassword,
            VipTier = 2,
            TotalSpending = 7500m,
            VipUpgradedAt = DateTime.UtcNow
        };
        
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/token", new { email, password });
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var userElement = jsonDoc.RootElement.GetProperty("data").GetProperty("user");
        
        // Verify all required fields exist
        userElement.TryGetProperty("id", out _).Should().BeTrue("User should have id field");
        userElement.TryGetProperty("fullName", out _).Should().BeTrue("User should have fullName field");
        userElement.TryGetProperty("email", out _).Should().BeTrue("User should have email field");
        userElement.TryGetProperty("isVip", out _).Should().BeTrue("User should have isVip field");
        userElement.TryGetProperty("vipTier", out _).Should().BeTrue("User should have vipTier field");
        userElement.TryGetProperty("totalSpending", out _).Should().BeTrue("User should have totalSpending field");
    }
}
