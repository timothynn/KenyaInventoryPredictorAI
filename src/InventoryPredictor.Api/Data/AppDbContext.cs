// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using InventoryPredictor.Shared.Models;

namespace InventoryPredictor.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<SalesTransaction> SalesTransactions { get; set; }
    public DbSet<StockAlert> StockAlerts { get; set; }
    public DbSet<PredictionResult> PredictionResults { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // InventoryItem configuration
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.Supplier).HasMaxLength(200);
            
            entity.Property(e => e.CurrentStock).HasPrecision(18, 2);
            entity.Property(e => e.MinimumStock).HasPrecision(18, 2);
            entity.Property(e => e.MaximumStock).HasPrecision(18, 2);
            entity.Property(e => e.ReorderPoint).HasPrecision(18, 2);
            entity.Property(e => e.OptimalOrderQuantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.DaysOfStockRemaining).HasPrecision(18, 2);

            entity.HasIndex(e => e.ProductCode).IsUnique();
            entity.HasIndex(e => e.Location);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
        });

        // SalesTransaction configuration
        modelBuilder.Entity<SalesTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.TransactionId).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Channel).HasMaxLength(50);
            entity.Property(e => e.CustomerId).HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);

            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.Location);
            entity.HasIndex(e => e.TransactionId);
        });

        // StockAlert configuration
        modelBuilder.Entity<StockAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.RecommendedAction).HasMaxLength(500);

            entity.Property(e => e.CurrentStock).HasPrecision(18, 2);
            entity.Property(e => e.ThresholdValue).HasPrecision(18, 2);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.IsResolved);
            entity.HasIndex(e => e.CreatedAt);
        });

        // PredictionResult configuration
        modelBuilder.Entity<PredictionResult>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.PredictionDate });
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductName).HasMaxLength(200);

            entity.Property(e => e.PredictedDemand7Days).HasPrecision(18, 2);
            entity.Property(e => e.PredictedDemand14Days).HasPrecision(18, 2);
            entity.Property(e => e.PredictedDemand30Days).HasPrecision(18, 2);
            entity.Property(e => e.CurrentStock).HasPrecision(18, 2);
            entity.Property(e => e.StockOutProbability).HasPrecision(5, 4);
            entity.Property(e => e.RecommendedOrderQuantity).HasPrecision(18, 2);
            entity.Property(e => e.ConfidenceScore).HasPrecision(5, 4);

            // Store complex objects as JSON
            entity.Property(e => e.DemandForecast)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<DateTime, decimal>>(v, (System.Text.Json.JsonSerializerOptions)null));

            entity.Property(e => e.Insights)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<string[]>(v, (System.Text.Json.JsonSerializerOptions)null));

            entity.Property(e => e.SeasonalityPattern)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions)null));

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.PredictionDate);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(50);

            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Location configuration
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LocationCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LocationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.County).HasMaxLength(100);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.OperatingHours).HasMaxLength(100);

            entity.Property(e => e.Latitude).HasPrecision(10, 7);
            entity.Property(e => e.Longitude).HasPrecision(10, 7);

            entity.HasIndex(e => e.LocationCode).IsUnique();
        });

        // Supplier configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SupplierCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.PaymentTerms).HasMaxLength(100);

            entity.HasIndex(e => e.SupplierCode).IsUnique();
        });
    }
}