namespace InventoryPredictor.Shared.Models;

// Additional response models
public class SalesSummary
{
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public decimal GrowthPercentage { get; set; }
    public List<TopProduct> TopProducts { get; set; } = new();
    public Dictionary<string, decimal> RevenueByCategory { get; set; } = new();
}
