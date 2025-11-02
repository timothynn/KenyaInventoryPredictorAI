using System.Net.Http.Json;

namespace InventoryPredictor.Api.Services;

public class FabricService : IFabricService
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<FabricService> _logger;
	private readonly HttpClient _httpClient;

	public FabricService(IConfiguration configuration, ILogger<FabricService> logger, HttpClient httpClient)
	{
		_configuration = configuration;
		_logger = logger;
		_httpClient = httpClient;
	}

	public async Task<List<Dictionary<string, object>>> ExecuteKqlQueryAsync(string query, Dictionary<string, object>? parameters = null)
	{
		try
		{
			var eventhouseEndpoint = _configuration["Fabric:EventhouseEndpoint"];
			var database = _configuration["Fabric:Database"];

			if (parameters != null)
			{
				foreach (var param in parameters)
				{
					query = query.Replace($"@{param.Key}", FormatParameter(param.Value));
				}
			}

			var requestBody = new
			{
				db = database,
				csl = query
			};

			var response = await _httpClient.PostAsJsonAsync($"{eventhouseEndpoint}/v1/rest/query", requestBody);
			response.EnsureSuccessStatusCode();

			var result = await response.Content.ReadFromJsonAsync<KustoQueryResult>();

			return ConvertKustoResultToDict(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error executing KQL query");
			throw;
		}
	}

	public async Task PublishEventAsync(string eventHubName, object eventData)
	{
		try
		{
			var eventHubConnectionString = _configuration["EventHub:ConnectionString"];
			_logger.LogInformation("Publishing event to {EventHub}", eventHubName);
			await Task.CompletedTask;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error publishing event to Event Hub {EventHub}", eventHubName);
			throw;
		}
	}

	private string FormatParameter(object value)
	{
		return value switch
		{
			string s => $"\"{s}\"",
			int or long or decimal or double => value.ToString(),
			bool b => b.ToString().ToLower(),
			DateTime dt => $"datetime({dt:yyyy-MM-ddTHH:mm:ss})",
			_ => value is null ? "null" : value.ToString()
		};
	}

	private List<Dictionary<string, object>> ConvertKustoResultToDict(KustoQueryResult? result)
	{
		var rows = new List<Dictionary<string, object>>();

		if (result?.Tables == null || !result.Tables.Any())
			return rows;

		var table = result.Tables[0];
		var columnNames = table.Columns.Select(c => c.ColumnName).ToList();

		foreach (var row in table.Rows)
		{
			var rowDict = new Dictionary<string, object>();
			for (int i = 0; i < columnNames.Count; i++)
			{
				rowDict[columnNames[i]] = row[i];
			}
			rows.Add(rowDict);
		}

		return rows;
	}
}

