// Services/EventHubService.cs
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Text.Json;

namespace InventoryPredictor.Api.Services;

public interface IEventHubService
{
    Task PublishInventoryEventAsync(object eventData);
    Task PublishSalesEventAsync(object eventData);
    Task PublishAlertEventAsync(object eventData);
}

public class EventHubService : IEventHubService, IAsyncDisposable
{
    private readonly EventHubProducerClient? _inventoryProducerClient;
    private readonly EventHubProducerClient? _salesProducerClient;
    private readonly EventHubProducerClient? _alertProducerClient;
    private readonly ILogger<EventHubService> _logger;
    private readonly bool _isEnabled;

    public EventHubService(IConfiguration configuration, ILogger<EventHubService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["EventHub:ConnectionString"];
        _isEnabled = !string.IsNullOrWhiteSpace(connectionString);

        if (_isEnabled)
        {
            try
            {
                var inventoryEventHubName = configuration["EventHub:InventoryEventHubName"] ?? "inventory-updates";
                var salesEventHubName = configuration["EventHub:SalesEventHubName"] ?? "sales-transactions";
                var alertEventHubName = configuration["EventHub:AlertEventHubName"] ?? "alert-events";

                _inventoryProducerClient = new EventHubProducerClient(connectionString, inventoryEventHubName);
                _salesProducerClient = new EventHubProducerClient(connectionString, salesEventHubName);
                _alertProducerClient = new EventHubProducerClient(connectionString, alertEventHubName);

                _logger.LogInformation("Event Hub Service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Event Hub clients");
                _isEnabled = false;
            }
        }
        else
        {
            _logger.LogWarning("Event Hub connection string not configured. Events will not be published.");
        }
    }

    public async Task PublishInventoryEventAsync(object eventData)
    {
        if (!_isEnabled || _inventoryProducerClient == null)
        {
            _logger.LogDebug("Event Hub not enabled. Skipping inventory event publishing.");
            return;
        }

        try
        {
            var eventJson = JsonSerializer.Serialize(eventData);
            var eventDataBytes = Encoding.UTF8.GetBytes(eventJson);

            using var eventBatch = await _inventoryProducerClient.CreateBatchAsync();
            var eventDataInstance = new EventData(eventDataBytes);
            
            // Add metadata
            eventDataInstance.Properties.Add("eventType", "InventoryUpdate");
            eventDataInstance.Properties.Add("timestamp", DateTime.UtcNow.ToString("O"));

            if (!eventBatch.TryAdd(eventDataInstance))
            {
                throw new Exception("Event is too large for the batch");
            }

            await _inventoryProducerClient.SendAsync(eventBatch);
            _logger.LogInformation("Published inventory event to Event Hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing inventory event to Event Hub");
            throw;
        }
    }

    public async Task PublishSalesEventAsync(object eventData)
    {
        if (!_isEnabled || _salesProducerClient == null)
        {
            _logger.LogDebug("Event Hub not enabled. Skipping sales event publishing.");
            return;
        }

        try
        {
            var eventJson = JsonSerializer.Serialize(eventData);
            var eventDataBytes = Encoding.UTF8.GetBytes(eventJson);

            using var eventBatch = await _salesProducerClient.CreateBatchAsync();
            var eventDataInstance = new EventData(eventDataBytes);
            
            eventDataInstance.Properties.Add("eventType", "SalesTransaction");
            eventDataInstance.Properties.Add("timestamp", DateTime.UtcNow.ToString("O"));

            if (!eventBatch.TryAdd(eventDataInstance))
            {
                throw new Exception("Event is too large for the batch");
            }

            await _salesProducerClient.SendAsync(eventBatch);
            _logger.LogInformation("Published sales event to Event Hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing sales event to Event Hub");
            throw;
        }
    }

    public async Task PublishAlertEventAsync(object eventData)
    {
        if (!_isEnabled || _alertProducerClient == null)
        {
            _logger.LogDebug("Event Hub not enabled. Skipping alert event publishing.");
            return;
        }

        try
        {
            var eventJson = JsonSerializer.Serialize(eventData);
            var eventDataBytes = Encoding.UTF8.GetBytes(eventJson);

            using var eventBatch = await _alertProducerClient.CreateBatchAsync();
            var eventDataInstance = new EventData(eventDataBytes);
            
            eventDataInstance.Properties.Add("eventType", "Alert");
            eventDataInstance.Properties.Add("timestamp", DateTime.UtcNow.ToString("O"));

            if (!eventBatch.TryAdd(eventDataInstance))
            {
                throw new Exception("Event is too large for the batch");
            }

            await _alertProducerClient.SendAsync(eventBatch);
            _logger.LogInformation("Published alert event to Event Hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing alert event to Event Hub");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_inventoryProducerClient != null)
            await _inventoryProducerClient.DisposeAsync();

        if (_salesProducerClient != null)
            await _salesProducerClient.DisposeAsync();

        if (_alertProducerClient != null)
            await _alertProducerClient.DisposeAsync();
    }
}

// Services/DataAgentService.cs
public interface IDataAgentService
{
    Task<DataAgentResponse> QueryAsync(string question, Dictionary<string, object> context);
    Task<string> GenerateReportAsync(string reportType, Dictionary<string, object> parameters);
}

public class DataAgentService : IDataAgentService
{
    private readonly IFabricService _fabricService;
    private readonly ILogger<DataAgentService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _dataAgentEndpoint;

    public DataAgentService(
        IFabricService fabricService,
        ILogger<DataAgentService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _fabricService = fabricService;
        _logger = logger;
        _httpClient = httpClient;
        _dataAgentEndpoint = configuration["Fabric:DataAgentEndpoint"] ?? "";
    }

    public async Task<DataAgentResponse> QueryAsync(string question, Dictionary<string, object> context)
    {
        try
        {
            _logger.LogInformation("Processing Data Agent query: {Question}", question);

            // Parse the question and determine intent
            var intent = DetermineIntent(question);

            // Execute appropriate KQL query based on intent
            var kqlQuery = GenerateKqlQuery(intent, question, context);
            var results = await _fabricService.ExecuteKqlQueryAsync(kqlQuery);

            // Generate natural language response
            var answer = GenerateNaturalLanguageResponse(intent, results, question);

            return new DataAgentResponse
            {
                Answer = answer,
                Data = results,
                Confidence = 0.85m,
                Suggestions = GenerateSuggestions(intent, results)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Data Agent query");
            return new DataAgentResponse
            {
                Answer = "I encountered an error processing your question. Please try rephrasing it.",
                Confidence = 0,
                Suggestions = new[] { "Try asking about specific products or locations" }
            };
        }
    }

    public async Task<string> GenerateReportAsync(string reportType, Dictionary<string, object> parameters)
    {
        try
        {
            _logger.LogInformation("Generating report: {ReportType}", reportType);

            var query = reportType switch
            {
                "weekly-summary" => GenerateWeeklySummaryQuery(parameters),
                "low-stock" => GenerateLowStockQuery(parameters),
                "sales-analysis" => GenerateSalesAnalysisQuery(parameters),
                _ => throw new ArgumentException($"Unknown report type: {reportType}")
            };

            var results = await _fabricService.ExecuteKqlQueryAsync(query);
            return FormatReportAsMarkdown(reportType, results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            throw;
        }
    }

    private string DetermineIntent(string question)
    {
        var lowerQuestion = question.ToLower();

        if (lowerQuestion.Contains("low stock") || lowerQuestion.Contains("running low"))
            return "low_stock";
        
        if (lowerQuestion.Contains("reorder") || lowerQuestion.Contains("order"))
            return "reorder";
        
        if (lowerQuestion.Contains("sales") || lowerQuestion.Contains("sold"))
            return "sales_analysis";
        
        if (lowerQuestion.Contains("prediction") || lowerQuestion.Contains("forecast"))
            return "prediction";
        
        if (lowerQuestion.Contains("alert") || lowerQuestion.Contains("warning"))
            return "alerts";

        return "general_query";
    }

    private string GenerateKqlQuery(string intent, string question, Dictionary<string, object> context)
    {
        var location = context.ContainsKey("location") ? context["location"].ToString() : "";

        return intent switch
        {
            "low_stock" => $@"
                inventory_levels
                | where current_stock < minimum_stock
                | where location == '{location}' or '{location}' == ''
                | project product_name, current_stock, minimum_stock, location, 
                    stock_deficit = minimum_stock - current_stock
                | order by stock_deficit desc
                | take 10",

            "reorder" => $@"
                let demand = sales_transactions
                | where transaction_date > ago(30d)
                | summarize avg_daily_demand = sum(quantity) / 30.0 by product_id;
                inventory_levels
                | join kind=inner demand on product_id
                | where current_stock / avg_daily_demand < 14
                | extend recommended_order = avg_daily_demand * 30
                | project product_name, current_stock, recommended_order, 
                    days_remaining = current_stock / avg_daily_demand
                | order by days_remaining asc",

            "sales_analysis" => $@"
                sales_transactions
                | where transaction_date > ago(7d)
                | summarize 
                    total_sales = sum(total_amount),
                    total_quantity = sum(quantity),
                    transaction_count = count()
                    by product_name
                | order by total_sales desc
                | take 10",

            _ => "inventory_levels | summarize count()"
        };
    }

    private string GenerateNaturalLanguageResponse(string intent, List<Dictionary<string, object>> results, string question)
    {
        if (!results.Any())
            return "I couldn't find any relevant data for your question.";

        return intent switch
        {
            "low_stock" => $"I found {results.Count} products running low on stock. " +
                          $"The most critical is {results.First()["product_name"]} with only {results.First()["current_stock"]} units remaining.",
            
            "reorder" => $"Based on current demand patterns, {results.Count} products need reordering. " +
                        $"Top priority is {results.First()["product_name"]} - you should order approximately {results.First()["recommended_order"]} units.",
            
            "sales_analysis" => $"In the past week, {results.First()["product_name"]} was the top seller " +
                               $"with {results.First()["total_sales"]} in revenue from {results.First()["transaction_count"]} transactions.",
            
            _ => $"I found {results.Count} relevant items matching your query."
        };
    }

    private string[] GenerateSuggestions(string intent, List<Dictionary<string, object>> results)
    {
        return intent switch
        {
            "low_stock" => new[]
            {
                "Would you like me to create purchase orders for these items?",
                "Should I show you predicted stock-out dates?",
                "Want to see demand forecasts for these products?"
            },
            "reorder" => new[]
            {
                "Shall I generate a complete purchase order?",
                "Would you like cost estimates for these orders?",
                "Should I check supplier availability?"
            },
            _ => new[]
            {
                "Would you like more details?",
                "Should I show related products?",
                "Want to see historical trends?"
            }
        };
    }

    private string GenerateWeeklySummaryQuery(Dictionary<string, object> parameters)
    {
        return @"
            sales_transactions
            | where transaction_date > ago(7d)
            | summarize 
                total_revenue = sum(total_amount),
                total_transactions = count(),
                unique_products = dcount(product_id),
                unique_customers = dcount(customer_id)
            | extend avg_transaction_value = total_revenue / total_transactions";
    }

    private string GenerateLowStockQuery(Dictionary<string, object> parameters)
    {
        return @"
            inventory_levels
            | where current_stock < minimum_stock * 1.2
            | project product_code, product_name, current_stock, minimum_stock, location
            | order by current_stock / minimum_stock asc";
    }

    private string GenerateSalesAnalysisQuery(Dictionary<string, object> parameters)
    {
        return @"
            sales_transactions
            | where transaction_date > ago(30d)
            | summarize 
                total_sales = sum(total_amount),
                avg_daily_sales = sum(total_amount) / 30.0
                by bin(transaction_date, 1d)
            | order by transaction_date desc";
    }

    private string FormatReportAsMarkdown(string reportType, List<Dictionary<string, object>> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {reportType.Replace("-", " ").ToUpper()} Report");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();

        foreach (var result in results.Take(10))
        {
            foreach (var kvp in result)
            {
                sb.AppendLine($"- **{kvp.Key}**: {kvp.Value}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public class DataAgentResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public decimal Confidence { get; set; }
    public string[] Suggestions { get; set; } = Array.Empty<string>();
}