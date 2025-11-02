// Controllers/PredictionsController.cs
using Microsoft.AspNetCore.Mvc;
using InventoryPredictor.Api.Services;
using InventoryPredictor.Shared.Models;
using InventoryPredictor.Shared.DTOs;

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
 
