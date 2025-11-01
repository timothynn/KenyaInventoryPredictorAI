// src/InventoryPredictor.Api/Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventoryPredictor.Api.Data;
using InventoryPredictor.Api.Services;
using InventoryPredictor.Api.Hubs;
using InventoryPredictor.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() 
    { 
        Title = "Kenya Inventory Predictor API", 
        Version = "v1",
        Description = "AI-Powered Inventory Management API",
        Contact = new() 
        { 
            Name = "Support",
            Email = "support@inventorypredictor.com"
        }
    });
});

// Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"] ?? "YourSecretKeyHere");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// SignalR
builder.Services.AddSignalR();

// HttpClient for external APIs
builder.Services.AddHttpClient();

// Register Application Services
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IFabricService, FabricService>();
builder.Services.AddScoped<IEventHubService, EventHubService>();
builder.Services.AddScoped<IDataAgentService, DataAgentService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddCheck("Fabric", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Fabric connected"));

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsProduction())
{
    builder.Logging.AddApplicationInsights();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kenya Inventory Predictor API v1");
        options.RoutePrefix = string.Empty; // Swagger at root
    });
}

// Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// API Controllers
app.MapControllers();

// SignalR Hub
app.MapHub<InventoryHub>("/hubs/inventory");

// Seed database if requested
if (args.Contains("--seed-data"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(context);
}

app.Run();