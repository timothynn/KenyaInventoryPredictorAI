namespace InventoryPredictor.Shared.Models;

// Models/InventoryAnalytics.cs
public class InventoryAnalytics
{
    public DateTime AnalysisDate { get; set; }
    public string Location { get; set; }
    
    // Overall metrics
    public decimal TotalInventoryValue { get; set; }
    public int TotalProductCount { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int OverstockedProducts { get; set; }
    
    // Performance metrics
    public decimal InventoryTurnoverRate { get; set; }
    public decimal DaysOfInventoryOnHand { get; set; }
    public decimal StockoutRate { get; set; }
    
    // Top products
    public List<TopProduct> TopSellingProducts { get; set; }
    public List<TopProduct> SlowMovingProducts { get; set; }
    
    // Financial
    public decimal TotalSalesRevenue { get; set; }
    public decimal ProfitMargin { get; set; }
}
