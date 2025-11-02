namespace InventoryPredictor.Shared.Models;

// Models/DemandPattern.cs
public class DemandPattern
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public decimal AverageDailyDemand { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal MaxDemand { get; set; }
    public decimal MinDemand { get; set; }
    public string DemandTrend { get; set; } // "Increasing", "Decreasing", "Stable"
    public List<SeasonalFactor> SeasonalFactors { get; set; }
    public int DataPointsAnalyzed { get; set; }
    public DateTime AnalysisDate { get; set; }
}
