namespace InventoryPredictor.Shared.Models;
// Models/TopProduct.cs
public class TopProduct
{
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public decimal TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
}
