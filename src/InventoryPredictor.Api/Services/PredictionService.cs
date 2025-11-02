using InventoryPredictor.Shared.Models;

namespace InventoryPredictor.Api.Services;

public class PredictionService : IPredictionService
{
	private readonly IFabricService _fabricService;
	private readonly ILogger<PredictionService> _logger;

	public PredictionService(IFabricService fabricService, ILogger<PredictionService> logger)
	{
		_fabricService = fabricService;
		_logger = logger;
	}

	public async Task<PredictionResult> GetDemandForecastAsync(Guid productId, int days)
	{
		var query = $@"
			// Get historical sales data
			let historical = sales_transactions
			| where product_id == '{productId}'
			| where transaction_date > ago(90d)
			| summarize quantity_sold = sum(quantity) by bin(transaction_date, 1d)
			| order by transaction_date asc;
            
			historical
		";

		var historicalData = await _fabricService.ExecuteKqlQueryAsync(query);

		var prediction = new PredictionResult
		{
			ProductId = productId,
			PredictionDate = DateTime.UtcNow,
		};

		return prediction;
	}

	public async Task<List<PredictionResult>> GenerateStockOutPredictionsAsync(
		List<Dictionary<string, object>> inventoryData,
		List<Dictionary<string, object>> salesData,
		int daysAhead)
	{
		var predictions = new List<PredictionResult>();

		foreach (var item in inventoryData)
		{
			var productId = Guid.Parse(item["product_id"].ToString());
			var currentStock = Convert.ToDecimal(item["current_stock"]);

			var sales = salesData.FirstOrDefault(s => s["product_id"].ToString() == productId.ToString());
			var avgDailyDemand = sales != null ? Convert.ToDecimal(sales["avg_daily_demand"]) : 0;

			if (avgDailyDemand > 0)
			{
				var daysUntilStockOut = (int)(currentStock / avgDailyDemand);
				var stockOutProbability = CalculateStockOutProbability(daysUntilStockOut, daysAhead);

				if (daysUntilStockOut <= daysAhead)
				{
					predictions.Add(new PredictionResult
					{
						ProductId = productId,
						ProductName = item["product_name"].ToString(),
						CurrentStock = currentStock,
						DaysUntilStockOut = daysUntilStockOut,
						EstimatedStockOutDate = DateTime.UtcNow.AddDays(daysUntilStockOut),
						StockOutProbability = stockOutProbability,
						PredictedDemand7Days = avgDailyDemand * 7,
						PredictedDemand14Days = avgDailyDemand * 14,
						PredictedDemand30Days = avgDailyDemand * 30,
						RecommendedOrderQuantity = avgDailyDemand * 30,
						Insights = GenerateInsights(daysUntilStockOut, avgDailyDemand)
					});
				}
			}
		}

		return predictions.OrderBy(p => p.DaysUntilStockOut).ToList();
	}

	public async Task RunPredictionModelAsync(List<Guid> productIds, int forecastDays)
	{
		_logger.LogInformation("Running prediction model for {Count} products", productIds.Count);
		await Task.CompletedTask;
	}

	private decimal CalculateStockOutProbability(int daysUntilStockOut, int daysAhead)
	{
		if (daysUntilStockOut <= 0) return 1.0m;
		if (daysUntilStockOut > daysAhead) return 0.0m;
		return 1.0m - ((decimal)daysUntilStockOut / daysAhead);
	}

	private string[] GenerateInsights(int daysUntilStockOut, decimal avgDailyDemand)
	{
		var insights = new List<string>();

		if (daysUntilStockOut < 7)
		{
			insights.Add("⚠️ Critical: Stock will run out within a week");
		}
		else if (daysUntilStockOut < 14)
		{
			insights.Add("⚡ Urgent: Consider reordering within next few days");
		}

		if (avgDailyDemand > 10)
		{
			insights.Add("📈 High demand product - monitor closely");
		}

		insights.Add($"💡 Average daily sales: {avgDailyDemand:F1} units");

		return insights.ToArray();
	}
}

