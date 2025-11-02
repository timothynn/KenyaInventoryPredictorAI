namespace InventoryPredictor.Shared.Models;

// Models/InventoryItem.cs
public class InventoryItem
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public string Category { get; set; } // e.g., "Food", "Beverages", "Hardware"
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal OptimalOrderQuantity { get; set; }
    public string Unit { get; set; } // e.g., "kg", "pieces", "liters"
    public decimal UnitPrice { get; set; }
    public string Location { get; set; } // e.g., "Nairobi Warehouse", "Mombasa Store"
    public string Supplier { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime LastRestocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Calculated properties
    public decimal StockValue => CurrentStock * UnitPrice;
    public decimal DaysOfStockRemaining { get; set; }
    public StockStatus Status { get; set; }
}
