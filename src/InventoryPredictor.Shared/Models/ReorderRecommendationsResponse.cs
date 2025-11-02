namespace InventoryPredictor.Shared.Models;

public class ReorderRecommendationsResponse
{
    public List<ReorderRecommendation> Recommendations { get; set; } = new();
}
