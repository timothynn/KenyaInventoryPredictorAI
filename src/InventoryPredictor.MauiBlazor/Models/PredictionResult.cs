
// Models/PredictionResult.cs
public class PredictionResult
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public DateTime PredictionDate { get; set; }
    
    // Demand predictions
    public Dictionary<DateTime, decimal> DemandForecast { get; set; } // Next 30 days
    public decimal PredictedDemand7Days { get; set; }
    public decimal PredictedDemand14Days { get; set; }
    public decimal PredictedDemand30Days { get; set; }
    
    // Stock predictions
    public decimal CurrentStock { get; set; }
    public DateTime EstimatedStockOutDate { get; set; }
    public decimal StockOutProbability { get; set; } // 0-1
    public int DaysUntilStockOut { get; set; }
    
    // Recommendations
    public decimal RecommendedOrderQuantity { get; set; }
    public DateTime RecommendedOrderDate { get; set; }
    public decimal ConfidenceScore { get; set; } // 0-1
    public string[] Insights { get; set; }
    
    // Trend analysis
    public TrendDirection Trend { get; set; }
    public bool IsSeasonalProduct { get; set; }
    public Dictionary<string, object> SeasonalityPattern { get; set; }
}
