namespace Shop_ProjForWeb.Core.Application.Interfaces;

using FluentValidation.Results;

public interface IValidationService
{
    Task<ValidationResult> ValidateAsync<T>(T model);
    Task<ValidationResult> ValidateBusinessRulesAsync<T>(T model, string operation);
    ValidationResult ValidateRange(decimal value, decimal min, decimal max, string fieldName);
    ValidationResult ValidateRequired(object? value, string fieldName);
    ValidationResult ValidatePositive(decimal value, string fieldName);
    ValidationResult ValidatePercentage(int value, string fieldName);
}