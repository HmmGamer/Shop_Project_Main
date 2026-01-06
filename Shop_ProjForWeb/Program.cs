using Shop_ProjForWeb.Core.Application.Configuration;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Application.Services;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;
using Shop_ProjForWeb.Infrastructure.Persistent.Configurations;
using Shop_ProjForWeb.Infrastructure.Repositories;
using Shop_ProjForWeb.Presentation.Middleware;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Shop_ProjForWeb.Core.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddDbContext<SupermarketDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
var key = Encoding.UTF8.GetBytes(jwtOptions?.Key ?? string.Empty);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions?.Issuer,
        ValidAudience = jwtOptions?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IVipStatusHistoryRepository, VipStatusHistoryRepository>();

// Register Unit of Work
builder.Services.AddScoped<Shop_ProjForWeb.Core.Application.Interfaces.IUnitOfWork, Shop_ProjForWeb.Infrastructure.UnitOfWork>();

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

builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileUpload"));

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

// Ensure UploadedFiles directory exists
var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
if (!Directory.Exists(uploadFolder))
{
    Directory.CreateDirectory(uploadFolder);
}

app.UseMiddleware<GlobalExceptionMiddleware>();

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SupermarketDbContext>();
    
    // Only run migrations and seeding if using a relational database (not in-memory for tests)
    if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        db.Database.Migrate();
        await DbSeeder.SeedAsync(db);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable static file serving for uploaded images
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

// Add Health Check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// Make the implicit Program class accessible to integration tests
public partial class Program { }
