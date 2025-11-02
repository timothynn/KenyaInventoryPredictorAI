namespace InventoryPredictor.Shared.Models;
// Models/SeasonalFactor.cs
public class SeasonalFactor
{
    public string Period { get; set; } // "January", "Q1", "Week 1"
    public decimal Factor { get; set; } // Multiplier
    public string Description { get; set; }
}
