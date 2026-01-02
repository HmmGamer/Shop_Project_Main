namespace Shop_ProjForWeb.Core.Application.Services;

using FluentValidation;
using FluentValidation.Results;
using Shop_ProjForWeb.Core.Application.Interfaces;

public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ValidationResult> ValidateAsync<T>(T model)
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();
        if (validator == null)
        {
            return new ValidationResult();
        }

        return await validator.ValidateAsync(model);
    }

    public Task<ValidationResult> ValidateBusinessRulesAsync<T>(T model, string operation)
    {
        var result = new ValidationResult();
        
        // Add business rule validations based on model type and operation
        switch (model)
        {
            case Shop_ProjForWeb.Core.Application.DTOs.CreateProductDto productDto:
                ValidateProductBusinessRules(productDto, result);
                break;
            case Shop_ProjForWeb.Core.Application.DTOs.UpdateProductDto updateProductDto:
                ValidateUpdateProductBusinessRules(updateProductDto, result);
                break;
            case Shop_ProjForWeb.Core.Application.DTOs.CreateUserDto userDto:
                ValidateUserBusinessRules(userDto, result);
                break;
        }

        return Task.FromResult(result);
    }

    public ValidationResult ValidateRange(decimal value, decimal min, decimal max, string fieldName)
    {
        var result = new ValidationResult();
        if (value < min || value > max)
        {
            result.Errors.Add(new ValidationFailure(fieldName, $"{fieldName} must be between {min} and {max}"));
        }
        return result;
    }

    public ValidationResult ValidateRequired(object? value, string fieldName)
    {
        var result = new ValidationResult();
        if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
        {
            result.Errors.Add(new ValidationFailure(fieldName, $"{fieldName} is required"));
        }
        return result;
    }

    public ValidationResult ValidatePositive(decimal value, string fieldName)
    {
        var result = new ValidationResult();
        if (value <= 0)
        {
            result.Errors.Add(new ValidationFailure(fieldName, $"{fieldName} must be greater than zero"));
        }
        return result;
    }

    public ValidationResult ValidatePercentage(int value, string fieldName)
    {
        var result = new ValidationResult();
        if (value < 0 || value > 100)
        {
            result.Errors.Add(new ValidationFailure(fieldName, $"{fieldName} must be between 0 and 100"));
        }
        return result;
    }

    private void ValidateProductBusinessRules(Shop_ProjForWeb.Core.Application.DTOs.CreateProductDto productDto, ValidationResult result)
    {
        if (productDto.BasePrice <= 0)
        {
            result.Errors.Add(new ValidationFailure(nameof(productDto.BasePrice), "Base price must be greater than zero"));
        }

        if (productDto.DiscountPercent < 0 || productDto.DiscountPercent > 100)
        {
            result.Errors.Add(new ValidationFailure(nameof(productDto.DiscountPercent), "Discount percent must be between 0 and 100"));
        }

        if (productDto.InitialStock < 0)
        {
            result.Errors.Add(new ValidationFailure(nameof(productDto.InitialStock), "Initial stock cannot be negative"));
        }
    }

    private void ValidateUpdateProductBusinessRules(Shop_ProjForWeb.Core.Application.DTOs.UpdateProductDto productDto, ValidationResult result)
    {
        if (productDto.BasePrice.HasValue && productDto.BasePrice <= 0)
        {
            result.Errors.Add(new ValidationFailure(nameof(productDto.BasePrice), "Base price must be greater than zero"));
        }

        if (productDto.DiscountPercent.HasValue && (productDto.DiscountPercent < 0 || productDto.DiscountPercent > 100))
        {
            result.Errors.Add(new ValidationFailure(nameof(productDto.DiscountPercent), "Discount percent must be between 0 and 100"));
        }
    }

    private void ValidateUserBusinessRules(Shop_ProjForWeb.Core.Application.DTOs.CreateUserDto userDto, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(userDto.FullName))
        {
            result.Errors.Add(new ValidationFailure(nameof(userDto.FullName), "Full name is required"));
        }
        else if (userDto.FullName.Length < 2)
        {
            result.Errors.Add(new ValidationFailure(nameof(userDto.FullName), "Full name must be at least 2 characters"));
        }
        else if (userDto.FullName.Length > 100)
        {
            result.Errors.Add(new ValidationFailure(nameof(userDto.FullName), "Full name cannot exceed 100 characters"));
        }
    }
}