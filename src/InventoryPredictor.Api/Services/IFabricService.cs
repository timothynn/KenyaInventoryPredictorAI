namespace InventoryPredictor.Api.Services;

public interface IFabricService
{
	Task<List<Dictionary<string, object>>> ExecuteKqlQueryAsync(string query, Dictionary<string, object>? parameters = null);
	Task PublishEventAsync(string eventHubName, object eventData);
}

