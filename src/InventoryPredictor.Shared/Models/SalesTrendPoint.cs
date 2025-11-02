namespace InventoryPredictor.Shared.Models;
public class SalesTrendPoint
{
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
    public int TransactionCount { get; set; }
}
