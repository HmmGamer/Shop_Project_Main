using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Application.Services;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Register Services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<VipUpgradeService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrderCancellationService, OrderCancellationService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<Shop_ProjForWeb.Core.Domain.Interfaces.IOrderStateMachine, OrderStateMachine>();
builder.Services.AddScoped<Shop_ProjForWeb.Core.Domain.Interfaces.IVipStatusCalculator, VipStatusCalculator>();
builder.Services.AddScoped<Shop_ProjForWeb.Core.Domain.Interfaces.IDiscountCalculator, AdditiveDiscountCalculator>();

// Register Validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<ProductImageService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<AgifyService>();

builder.Services.AddControllers();

// Add Health Checks
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Shop Project API",
        Version = "v1",
        Description = "A comprehensive e-commerce API with inventory management, order processing, and VIP customer features",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Shop Project Team"
        }
    });
    
    // Enable XML comments for better Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Add Health Check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();