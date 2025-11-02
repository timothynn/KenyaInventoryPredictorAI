// SeedData.cs
using Microsoft.EntityFrameworkCore;
using InventoryPredictor.Shared.Models;

namespace InventoryPredictor.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (context.InventoryItems.Any())
        {
            Console.WriteLine("Database already seeded.");
            return;
        }

        Console.WriteLine("Seeding database...");

        // Seed Products
        var products = new List<InventoryItem>
        {
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductCode = "P001",
                ProductName = "Maize Flour 2kg",
                Category = "Food",
                CurrentStock = 250,
                MinimumStock = 100,
                ReorderPoint = 120,
                OptimalOrderQuantity = 300,
                Unit = "kg",
                UnitPrice = 150.00m,
                Location = "Nairobi_Warehouse",
                Supplier = "Unga Limited",
                LeadTimeDays = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = StockStatus.Optimal
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductCode = "P002",
                ProductName = "Cooking Oil 1L",
                Category = "Food",
                CurrentStock = 45,
                MinimumStock = 60,
                ReorderPoint = 80,
                OptimalOrderQuantity = 200,
                Unit = "liters",
                UnitPrice = 320.00m,
                Location = "Nairobi_Warehouse",
                Supplier = "Bidco Africa",
                LeadTimeDays = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = StockStatus.Low
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductCode = "P003",
                ProductName = "Sugar 1kg",
                Category = "Food",
                CurrentStock = 320,
                MinimumStock = 150,
                ReorderPoint = 200,
                OptimalOrderQuantity = 400,
                Unit = "kg",
                UnitPrice = 120.00m,
                Location = "Nairobi_Warehouse",
                Supplier = "Mumias Sugar",
                LeadTimeDays = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = StockStatus.Optimal
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductCode = "P004",
                ProductName = "Rice 5kg",
                Category = "Food",
                CurrentStock = 150,
                MinimumStock = 80,
                ReorderPoint = 100,
                OptimalOrderQuantity = 250,
                Unit = "kg",
                UnitPrice = 650.00m,
                Location = "Nairobi_Warehouse",
                Supplier = "Mwea Rice Mills",
                LeadTimeDays = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = StockStatus.Optimal
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductCode = "P005",
                ProductName = "Milk 500ml",
                Category = "Dairy",
                CurrentStock = 25,
                MinimumStock = 120,
                ReorderPoint = 150,
                OptimalOrderQuantity = 300,
                Unit = "pieces",
                UnitPrice = 60.00m,
                Location = "Nairobi_Warehouse",
                Supplier = "Brookside Dairy",
                LeadTimeDays = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = StockStatus.CriticallyLow
            }
        };

        context.InventoryItems.AddRange(products);

        // Seed Sales Transactions
        var random = new Random();
    var transactions = new List<SalesTransaction>();

        for (int i = 0; i < 1000; i++)
        {
            var product = products[random.Next(products.Count)];
            var daysAgo = random.Next(0, 90);
            var quantity = random.Next(1, 50);

            transactions.Add(new SalesTransaction
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                TotalAmount = quantity * product.UnitPrice,
                TransactionDate = DateTime.UtcNow.AddDays(-daysAgo),
                Location = product.Location,
                Channel = GetRandomChannel(random),
                CustomerId = $"CUST{random.Next(1, 100):D3}",
                PaymentMethod = GetRandomPaymentMethod(random),
                TransactionId = $"TXN{Guid.NewGuid():N}",
                Type = TransactionType.Sale
            });
        }

        context.SalesTransactions.AddRange(transactions);
        await context.SaveChangesAsync();

        Console.WriteLine($"Seeded {products.Count} products and {transactions.Count} transactions.");
    }

    private static string GetRandomChannel(Random random)
    {
        var channels = new[] { "Store", "Mobile", "Online", "Wholesale" };
        return channels[random.Next(channels.Length)];
    }

    private static string GetRandomPaymentMethod(Random random)
    {
        var weights = new[] { 65, 25, 8, 2 }; // M-Pesa most common
        var methods = new[] { "M-Pesa", "Cash", "Card", "Bank_Transfer" };
        
        var total = weights.Sum();
        var randomNumber = random.Next(total);
        var cumulative = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (randomNumber < cumulative)
                return methods[i];
        }

        return methods[0];
    }
}