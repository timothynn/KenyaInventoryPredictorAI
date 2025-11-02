namespace InventoryPredictor.Shared.Models;

public enum AlertType
{
    LowStock,
    StockOut,
    PredictedStockOut,
    Overstock,
    AnomalySale,
    ReorderRecommendation,
    ExpiringStock
}

