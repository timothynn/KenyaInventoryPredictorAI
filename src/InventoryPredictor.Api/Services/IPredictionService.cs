using InventoryPredictor.Shared.Models;

namespace InventoryPredictor.Api.Services;

public interface IPredictionService
{
	Task<PredictionResult> GetDemandForecastAsync(Guid productId, int days);
	Task<List<PredictionResult>> GenerateStockOutPredictionsAsync(
		List<Dictionary<string, object>> inventoryData,
		List<Dictionary<string, object>> salesData,
		int daysAhead);
	Task RunPredictionModelAsync(List<Guid> productIds, int forecastDays);
}

