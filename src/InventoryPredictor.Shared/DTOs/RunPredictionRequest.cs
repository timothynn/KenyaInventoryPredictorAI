namespace InventoryPredictor.Shared.DTOs;

public class RunPredictionRequest
{
    public List<Guid> ProductIds { get; set; } = new();
    public int ForecastDays { get; set; }
    public bool IncludeSeasonality { get; set; }
}
