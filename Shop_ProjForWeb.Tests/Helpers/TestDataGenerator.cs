namespace Shop_ProjForWeb.Tests.Helpers;

using Bogus;
using Shop_ProjForWeb.Core.Application.DTOs;
using System;
using System.Collections.Generic;

public static class TestDataGenerator
{
    private static readonly Faker _faker = new Faker();

    // User Generators
    public static CreateUserDto GenerateValidUser()
    {
        return new CreateUserDto
        {
            FullName = _faker.Name.FullName()
        };
    }

    public static UpdateUserDto GenerateValidUserUpdate()
    {
        return new UpdateUserDto
        {
            FullName = _faker.Name.FullName()
        };
    }

    public static CreateUserDto GenerateInvalidUser(string invalidField)
    {
        return invalidField.ToLower() switch
        {
            "email" => new CreateUserDto { FullName = "invalid-email@" },
            "shortname" => new CreateUserDto { FullName = "A" },
            "emptyname" => new CreateUserDto { FullName = "" },
            _ => GenerateValidUser()
        };
    }

    // Product Generators
    public static CreateProductDto GenerateValidProduct()
    {
        return new CreateProductDto
        {
            Name = _faker.Commerce.ProductName(),
            BasePrice = decimal.Parse(_faker.Commerce.Price(1, 1000)),
            DiscountPercent = _faker.Random.Int(0, 50),
            IsActive = true,
            InitialStock = _faker.Random.Int(10, 100)
        };
    }

    public static UpdateProductDto GenerateValidProductUpdate()
    {
        return new UpdateProductDto
        {
            Name = _faker.Commerce.ProductName(),
            BasePrice = decimal.Parse(_faker.Commerce.Price(1, 1000)),
            DiscountPercent = _faker.Random.Int(0, 50),
            IsActive = _faker.Random.Bool()
        };
    }

    public static CreateProductDto GenerateInvalidProduct(string invalidField)
    {
        return invalidField.ToLower() switch
        {
            "negativeprice" => new CreateProductDto
            {
                Name = _faker.Commerce.ProductName(),
                BasePrice = -10.00m,
                DiscountPercent = 0,
                IsActive = true,
                InitialStock = 10
            },
            "invaliddiscount" => new CreateProductDto
            {
                Name = _faker.Commerce.ProductName(),
                BasePrice = 100.00m,
                DiscountPercent = 150,
                IsActive = true,
                InitialStock = 10
            },
            "zeroprice" => new CreateProductDto
            {
                Name = _faker.Commerce.ProductName(),
                BasePrice = 0m,
                DiscountPercent = 0,
                IsActive = true,
                InitialStock = 10
            },
            _ => GenerateValidProduct()
        };
    }

    // Order Generators
    public static List<CreateOrderItemDto> GenerateValidOrderItems(List<Guid> productIds, int? quantity = null)
    {
        var items = new List<CreateOrderItemDto>();
        
        foreach (var productId in productIds)
        {
            items.Add(new CreateOrderItemDto
            {
                ProductId = productId,
                Quantity = quantity ?? _faker.Random.Int(1, 5)
            });
        }

        return items;
    }

    public static List<CreateOrderItemDto> GenerateInvalidOrderItems(string invalidField)
    {
        return invalidField.ToLower() switch
        {
            "empty" => new List<CreateOrderItemDto>(),
            "zeroquantity" => new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 0
                }
            },
            "negativequantity" => new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = -5
                }
            },
            _ => new List<CreateOrderItemDto>()
        };
    }

    // Inventory Generators
    public static CreateInventoryDto GenerateValidInventory(Guid productId)
    {
        return new CreateInventoryDto
        {
            ProductId = productId,
            Quantity = _faker.Random.Int(10, 100)
        };
    }

    public static UpdateInventoryDto GenerateValidInventoryUpdate()
    {
        return new UpdateInventoryDto
        {
            Quantity = _faker.Random.Int(10, 100)
        };
    }

    public static CreateInventoryDto GenerateInvalidInventory(string invalidField)
    {
        return invalidField.ToLower() switch
        {
            "negativequantity" => new CreateInventoryDto
            {
                ProductId = Guid.NewGuid(),
                Quantity = -10
            },
            _ => GenerateValidInventory(Guid.NewGuid())
        };
    }

    public static UpdateInventoryDto GenerateInvalidInventoryUpdate(string invalidField)
    {
        return invalidField.ToLower() switch
        {
            "negativequantity" => new UpdateInventoryDto
            {
                Quantity = -10
            },
            _ => GenerateValidInventoryUpdate()
        };
    }

    // Helper methods
    public static string GenerateInvalidEmail()
    {
        var invalidEmails = new[]
        {
            "notanemail",
            "@nodomain.com",
            "missing@",
            "spaces in@email.com",
            "double@@domain.com"
        };
        return _faker.PickRandom(invalidEmails);
    }

    public static string GenerateShortName()
    {
        return _faker.Random.String2(1);
    }
}
