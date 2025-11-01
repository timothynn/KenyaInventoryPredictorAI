
public class Location
{
    public Guid Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string OperatingHours { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}