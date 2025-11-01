// Request/Response models
public class StockMovementRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string Type { get; set; } = string.Empty; // Sale, Purchase, Adjustment, Transfer, etc.
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}