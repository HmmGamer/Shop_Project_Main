using Shop_ProjForWeb.Core.Application.Configuration;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Application.Services;
using Shop_ProjForWeb.Infrastructure.Persistent.DbContext;
using Shop_ProjForWeb.Infrastructure.Repositories;
using Shop_ProjForWeb.Presentation.Middleware;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.OpenApi.Models;

using FluentValidation;
using Serilog;
using System.Text;

Console.WriteLine(">>> [BOOT] Program start");

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine(">>> [BOOT] Builder created");

/* ======================= SERILOG ======================= */

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

Console.WriteLine(">>> [BOOT] Serilog configured");

/* ======================= DATABASE ======================= */

builder.Services.AddDbContext<SupermarketDbContext>(options =>
{
    Console.WriteLine(">>> [DI] Configuring DbContext");
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.CommandTimeout(5)
    );
});

Console.WriteLine(">>> [DI] DbContext registered");

/* ======================= JWT ======================= */

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();

Console.WriteLine(">>> [JWT] JwtOptions loaded");

if (string.IsNullOrWhiteSpace(jwtOptions?.Key))
{
    Console.WriteLine(">>> [JWT] ERROR: JWT key missing");
    throw new Exception("JWT configuration is missing or invalid");
}

var key = Encoding.UTF8.GetBytes(jwtOptions.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    Console.WriteLine(">>> [JWT] JwtBearer configured");

    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

Console.WriteLine(">>> [DI] Authentication configured");

/* ======================= HTTP CLIENT ======================= */

builder.Services.AddHttpClient();
Console.WriteLine(">>> [DI] HttpClient registered");

/* ======================= DI ======================= */

Console.WriteLine(">>> [DI] Registering repositories & services");

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IVipStatusHistoryRepository, VipStatusHistoryRepository>();

builder.Services.AddScoped<Shop_ProjForWeb.Core.Application.Interfaces.IUnitOfWork,
    Shop_ProjForWeb.Infrastructure.UnitOfWork>();

builder.Services.AddScoped<Shop_ProjForWeb.Core.Domain.Interfaces.IVipStatusCalculator, VipStatusCalculator>();
builder.Services.AddScoped<Shop_ProjForWeb.Core.Domain.Interfaces.IDiscountCalculator, AdditiveDiscountCalculator>();
builder.Services.AddScoped<Shop_ProjForWeb.Core.Domain.Interfaces.IOrderStateMachine, OrderStateMachine>();

builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<VipUpgradeService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderCancellationService, OrderCancellationService>();

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ProductImageService>();
builder.Services.AddScoped<AgifyService>();

Console.WriteLine(">>> [DI] Services registered");

/* ======================= VALIDATION / FILE STORAGE ======================= */

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<IValidationService, ValidationService>();

builder.Services.Configure<FileUploadOptions>(
    builder.Configuration.GetSection("FileUpload"));

builder.Services.AddScoped<IFileStorageService,
    Shop_ProjForWeb.Infrastructure.Services.LocalFileStorageService>();

Console.WriteLine(">>> [DI] Validation & File storage registered");

/* ======================= MVC / AUTH ======================= */

builder.Services.AddAuthorization();

builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

Console.WriteLine(">>> [DI] Controllers configured");

/* ======================= HEALTH ======================= */

builder.Services.AddHealthChecks();
Console.WriteLine(">>> [DI] Health checks added");

/* ======================= SWAGGER ======================= */

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    Console.WriteLine(">>> [DI] Swagger configuring");

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Shop Project API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

Console.WriteLine(">>> [DI] Swagger registered");

/* ======================= BUILD ======================= */

WebApplication app;

try
{
    Console.WriteLine(">>> [BUILD] Building app...");
    app = builder.Build();
    Console.WriteLine(">>> [BUILD] App built successfully");
}
catch (Exception ex)
{
    Console.WriteLine(">>> [BUILD] FAILED");
    Console.WriteLine(ex);
    throw;
}

/* ======================= LIFETIME DEBUG ======================= */

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine(">>> [LIFETIME] Application STARTED");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine(">>> [LIFETIME] Application STOPPING");
});

/* ======================= MIDDLEWARE ======================= */

Console.WriteLine(">>> [PIPELINE] Registering middleware");

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SerilogUserEnricherMiddleware>();
app.UseSerilogRequestLogging();

/* ======================= DATABASE MIGRATION ======================= */

Console.WriteLine(">>> [DB] Starting migration");

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SupermarketDbContext>();

    Console.WriteLine(">>> [DB] DbContext resolved");
    await db.Database.MigrateAsync();
    Console.WriteLine(">>> [DB] Migration completed");
    
    // Seed the database with initial data
    Console.WriteLine(">>> [DB] Starting seeding");
    await Shop_ProjForWeb.Infrastructure.Persistent.Configurations.DbSeeder.SeedAsync(db);
    Console.WriteLine(">>> [DB] Seeding completed");
}
catch (Exception ex)
{
    Console.WriteLine(">>> [DB] MIGRATION/SEEDING FAILED");
    Console.WriteLine(ex);
}

/* ======================= PIPELINE ======================= */

if (app.Environment.IsDevelopment())
{
    Console.WriteLine(">>> [PIPELINE] Development mode");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapFallbackToFile("index.html");

Console.WriteLine(">>> [PIPELINE] Starting app.Run()");
Console.WriteLine(">>> [FINAL] IF YOU SEE THIS, APP IS ALIVE");

app.Run();

public partial class Program { }
