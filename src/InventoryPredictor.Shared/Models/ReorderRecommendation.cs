
public class ReorderRecommendation
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal RecommendedOrderQuantity { get; set; }
    public DateTime RecommendedOrderDate { get; set; }
    public string Urgency { get; set; } = string.Empty; // High, Medium, Low
    public decimal EstimatedCost { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int DaysUntilStockOut { get; set; }
    public string Unit { get; set; } = string.Empty;
}