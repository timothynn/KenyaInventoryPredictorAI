// Controllers/PredictionsController.cs
using Microsoft.AspNetCore.Mvc;
using InventoryPredictor.Api.Services;
using InventoryPredictor.Shared.Models;

namespace InventoryPredictor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly IPredictionService _predictionService;
    private readonly IFabricService _fabricService;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(
        IPredictionService predictionService,
        IFabricService fabricService,
        ILogger<PredictionsController> logger)
    {
        _predictionService = predictionService;
        _fabricService = fabricService;
        _logger = logger;
    }

    /// <summary>
    /// Get demand forecast for a specific product
    /// </summary>
    [HttpGet("demand/{productId}")]
    [ProducesResponseType(typeof(ApiResponse<PredictionResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDemandForecast(
        [FromRoute] Guid productId,
        [FromQuery] int days = 30)
    {
        try
        {
            var prediction = await _predictionService.GetDemandForecastAsync(productId, days);
            
            if (prediction == null)
            {
                return NotFound(new ApiResponse<PredictionResult>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "PRODUCT_NOT_FOUND",
                        Message = $"Product with ID {productId} not found"
                    },
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new ApiResponse<PredictionResult>
            {
                Success = true,
                Data = prediction,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting demand forecast for product {ProductId}", productId);
            return StatusCode(500, new ApiResponse<PredictionResult>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching the demand forecast"
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get stock-out predictions for all products
    /// </summary>
    [HttpGet("stockout")]
    [ProducesResponseType(typeof(ApiResponse<StockOutPredictionsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockOutPredictions(
        [FromQuery] string? location = null,
        [FromQuery] int daysAhead = 30)
    {
        try
        {
            // Query Fabric Eventhouse for current inventory levels
            var inventoryQuery = @"
                inventory_levels
                | where location == @location or @location == """"
                | where current_stock <= minimum_stock * 2
                | project product_id, product_name, current_stock, minimum_stock, location
            ";

            var inventoryData = await _fabricService.ExecuteKqlQueryAsync(inventoryQuery, new Dictionary<string, object>
            {
                { "location", location ?? string.Empty }
            });

            // Query for historical sales to calculate demand
            var salesQuery = @"
                sales_transactions
                | where transaction_date > ago(90d)
                | where product_id in (@productIds)
                | summarize 
                    avg_daily_demand = sum(quantity) / 90.0,
                    std_dev = stdev(quantity)
                    by product_id
            ";

            var productIds = inventoryData.Select(row => row["product_id"].ToString()).ToList();
            var salesData = await _fabricService.ExecuteKqlQueryAsync(salesQuery, new Dictionary<string, object>
            {
                { "productIds", productIds }
            });

            // Generate predictions using ML model
            var predictions = await _predictionService.GenerateStockOutPredictionsAsync(
                inventoryData,
                salesData,
                daysAhead);

            return Ok(new ApiResponse<StockOutPredictionsResponse>
            {
                Success = true,
                Data = new StockOutPredictionsResponse
                {
                    Predictions = predictions
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock-out predictions");
            return StatusCode(500, new ApiResponse<StockOutPredictionsResponse>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while generating predictions"
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get reorder recommendations
    /// </summary>
    [HttpGet("reorder-recommendations")]
    [ProducesResponseType(typeof(ApiResponse<ReorderRecommendationsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReorderRecommendations()
    {
        try
        {
            // Query Fabric for products needing reorder
            var query = @"
                let demand_forecast = 
                    sales_transactions
                    | where transaction_date > ago(30d)
                    | summarize avg_demand_per_day = sum(quantity) / 30.0 by product_id;
                
                inventory_levels
                | join kind=inner demand_forecast on product_id
                | extend 
                    days_remaining = current_stock / avg_demand_per_day,
                    recommended_order_qty = avg_demand_per_day * 30,  // 30 days supply
                    urgency = case(
                        days_remaining < 7, ""High"",
                        days_remaining < 14, ""Medium"",
                        ""Low""
                    )
                | where days_remaining < 21  // Less than 3 weeks remaining
                | project 
                    product_id,
                    product_code,
                    product_name,
                    current_stock,
                    recommended_order_qty,
                    recommended_order_date = now() + todatetime(days_remaining - 7),
                    urgency,
                    estimated_cost = recommended_order_qty * unit_price,
                    reason = strcat(""Stock will run out in "", tostring(toint(days_remaining)), "" days based on current demand"")
                | order by days_remaining asc
            ";

            var queryResults = await _fabricService.ExecuteKqlQueryAsync(query);

            var recommendations = queryResults.Select(row => new ReorderRecommendation
            {
                ProductId = Guid.Parse(row["product_id"].ToString()),
                ProductCode = row["product_code"].ToString(),
                ProductName = row["product_name"].ToString(),
                CurrentStock = Convert.ToDecimal(row["current_stock"]),
                RecommendedOrderQuantity = Convert.ToDecimal(row["recommended_order_qty"]),
                RecommendedOrderDate = Convert.ToDateTime(row["recommended_order_date"]),
                Urgency = row["urgency"].ToString(),
                EstimatedCost = Convert.ToDecimal(row["estimated_cost"]),
                Reason = row["reason"].ToString()
            }).ToList();

            return Ok(new ApiResponse<ReorderRecommendationsResponse>
            {
                Success = true,
                Data = new ReorderRecommendationsResponse
                {
                    Recommendations = recommendations
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reorder recommendations");
            return StatusCode(500, new ApiResponse<ReorderRecommendationsResponse>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while generating recommendations"
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Run prediction model for specific products
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunPredictionModel([FromBody] RunPredictionRequest request)
    {
        try
        {
            // Trigger ML model execution in Fabric
            await _predictionService.RunPredictionModelAsync(request.ProductIds, request.ForecastDays);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { Message = "Prediction model started successfully" },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running prediction model");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while running the prediction model"
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

// Services/FabricService.cs
public interface IFabricService
{
    Task<List<Dictionary<string, object>>> ExecuteKqlQueryAsync(string query, Dictionary<string, object>? parameters = null);
    Task PublishEventAsync(string eventHubName, object eventData);
}

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
            // Execute KQL query against Fabric Eventhouse
            var eventhouseEndpoint = _configuration["Fabric:EventhouseEndpoint"];
            var database = _configuration["Fabric:Database"];

            // Replace parameters in query
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
            
            // Convert result to list of dictionaries
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
            // Implement Event Hub publishing logic here
            _logger.LogInformation("Publishing event to {EventHub}", eventHubName);
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
            _ => value.ToString()
        };
    }

    private List<Dictionary<string, object>> ConvertKustoResultToDict(KustoQueryResult result)
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

// Kusto result models
public class KustoQueryResult
{
    public List<KustoTable> Tables { get; set; } = new();
}

public class KustoTable
{
    public string TableName { get; set; } = string.Empty;
    public List<KustoColumn> Columns { get; set; } = new();
    public List<List<object>> Rows { get; set; } = new();
}

public class KustoColumn
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

// Services/PredictionService.cs (Backend implementation)
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
        // Call Fabric ML model endpoint for prediction
        // This would integrate with your deployed Prophet/ML model
        
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
        
        // Call ML model for prediction
        // In production, this would call your deployed Fabric ML model endpoint
        
        var prediction = new PredictionResult
        {
            ProductId = productId,
            PredictionDate = DateTime.UtcNow,
            // Generate forecast based on ML model output
            // This is simplified - actual implementation would call the ML model
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
            
            // Find matching sales data
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
                        RecommendedOrderQuantity = avgDailyDemand * 30, // 30 days supply
                        Insights = GenerateInsights(daysUntilStockOut, avgDailyDemand)
                    });
                }
            }
        }

        return predictions.OrderBy(p => p.DaysUntilStockOut).ToList();
    }

    public async Task RunPredictionModelAsync(List<Guid> productIds, int forecastDays)
    {
        // Trigger Fabric notebook execution or ML model endpoint
        _logger.LogInformation("Running prediction model for {Count} products", productIds.Count);
        
        // In production, this would:
        // 1. Trigger a Fabric pipeline or notebook
        // 2. Pass product IDs and parameters
        // 3. Wait for model execution
        // 4. Store results back to Eventhouse/Lakehouse
        
        await Task.CompletedTask;
    }

    private decimal CalculateStockOutProbability(int daysUntilStockOut, int daysAhead)
    {
        if (daysUntilStockOut <= 0) return 1.0m;
        if (daysUntilStockOut > daysAhead) return 0.0m;
        
        // Simple linear probability calculation
        // In production, use more sophisticated model
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
