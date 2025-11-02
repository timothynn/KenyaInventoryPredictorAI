namespace InventoryPredictor.Shared.Models;

// Models/StockAlert.cs
public class StockAlert
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsResolved { get; set; }
    public string Location { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ThresholdValue { get; set; }
    public string RecommendedAction { get; set; }
}
