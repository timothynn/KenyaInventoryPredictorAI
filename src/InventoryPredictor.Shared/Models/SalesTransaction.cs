namespace InventoryPredictor.Shared.Models;

public class SalesTransaction
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty; // e.g., "Store", "Online", "Mobile"
    public string CustomerId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty; // e.g., "M-Pesa", "Cash", "Card"
    public string TransactionId { get; set; } = string.Empty;
    public TransactionType Type { get; set; } // Sale, Return, Adjustment
    public string Notes { get; set; } = string.Empty;
}
