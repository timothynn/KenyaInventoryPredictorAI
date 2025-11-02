namespace InventoryPredictor.Shared.Models;

public class StockOutPredictionsResponse
{
    public List<PredictionResult> Predictions { get; set; } = new();
}
