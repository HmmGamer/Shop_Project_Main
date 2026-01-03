namespace Shop_ProjForWeb.Presentation.Middleware;

using System.Net;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Shop_ProjForWeb.Core.Domain.Exceptions;
using Shop_ProjForWeb.Core.Application.DTOs;

public class GlobalExceptionMiddleware(RequestDelegate next, IWebHostEnvironment environment, ILogger<GlobalExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new StandardErrorResponse
        {
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        // Log the exception with appropriate level
        var logLevel = GetLogLevel(exception);
        _logger.Log(logLevel, exception, "Exception occurred: {ExceptionType} - {Message}", 
            exception.GetType().Name, exception.Message);

        switch (exception)
        {
            case ValidationException validationEx:
                response.Error = "ValidationFailed";
                response.Message = "One or more validation errors occurred";
                response.ValidationErrors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case InsufficientStockException:
                response.Error = "InsufficientStock";
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case ProductNotFoundException:
            case UserNotFoundException:
            case OrderNotFoundException:
            case InventoryNotFoundException:
                response.Error = "ResourceNotFound";
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case ArgumentException:
                response.Error = "InvalidArgument";
                response.Message = SanitizeMessage(exception.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case InvalidOperationException:
                response.Error = "BusinessRuleViolation";
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                break;

            case InvalidDiscountException:
            case InvalidPriceException:
                response.Error = "ValidationError";
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case DbUpdateConcurrencyException:
                response.Error = "ConcurrencyConflict";
                response.Message = "The record was modified by another user. Please refresh and try again.";
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                break;

            case DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("FOREIGN KEY constraint failed") == true:
                response.Error = "ReferentialIntegrityViolation";
                response.Message = "Cannot perform this operation due to related data constraints.";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("UNIQUE constraint failed") == true:
                response.Error = "DuplicateResource";
                response.Message = "A resource with the same identifier already exists.";
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                break;

            case TimeoutException:
                response.Error = "RequestTimeout";
                response.Message = "The request timed out. Please try again.";
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                break;

            case UnauthorizedAccessException:
                response.Error = "Unauthorized";
                response.Message = "You are not authorized to perform this action.";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            default:
                response.Error = "InternalServerError";
                response.Message = "An unexpected error occurred. Please try again later.";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                
                // Include more details in development
                if (_environment.IsDevelopment())
                {
                    response.Message = exception.Message;
                    response.ValidationErrors = new Dictionary<string, string[]>
                    {
                        ["StackTrace"] = new[] { exception.StackTrace ?? "No stack trace available" }
                    };
                }
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });
        
        return context.Response.WriteAsync(jsonResponse);
    }

    private static LogLevel GetLogLevel(Exception exception)
    {
        return exception switch
        {
            ValidationException => LogLevel.Information,
            ArgumentException => LogLevel.Information,
            ProductNotFoundException => LogLevel.Information,
            UserNotFoundException => LogLevel.Information,
            OrderNotFoundException => LogLevel.Information,
            InventoryNotFoundException => LogLevel.Information,
            InsufficientStockException => LogLevel.Warning,
            InvalidOperationException => LogLevel.Warning,
            DbUpdateConcurrencyException => LogLevel.Warning,
            TimeoutException => LogLevel.Warning,
            UnauthorizedAccessException => LogLevel.Warning,
            _ => LogLevel.Error
        };
    }

    private static string SanitizeMessage(string message)
    {
        // Remove potentially sensitive information from error messages
        // This is a simple implementation - in production you might want more sophisticated sanitization
        return message.Length > 500 ? message[..500] + "..." : message;
    }
}
