
// Models/SalesTransaction.cs
public class SalesTransaction
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Location { get; set; }
    public string Channel { get; set; } // e.g., "Store", "Online", "Mobile"
    public string CustomerId { get; set; }
    public string PaymentMethod { get; set; } // e.g., "M-Pesa", "Cash", "Card"
    public string TransactionId { get; set; }
    public TransactionType Type { get; set; } // Sale, Return, Adjustment
    public string Notes { get; set; }
}

