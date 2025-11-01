// Services/InventoryService.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InventoryPredictor.Shared.Models;

namespace InventoryPredictor.MauiBlazor.Services;

public interface IInventoryService
{
    Task<List<InventoryItem>> GetAllInventoryAsync(string? location = null, string? category = null);
    Task<InventoryItem?> GetInventoryItemAsync(Guid id);
    Task<InventoryItem> UpdateInventoryAsync(Guid id, InventoryItem item);
    Task<List<StockAlert>> GetActiveAlertsAsync();
    Task<InventoryAnalytics> GetAnalyticsAsync(string? location = null);
    Task<SalesSummary> GetSalesSummaryAsync(DateTime startDate, DateTime endDate);
    Task<List<SalesTrendPoint>> GetSalesTrendsAsync(int days);
    Task<List<CategoryData>> GetInventoryByCategoryAsync();
    Task<List<ActivityItem>> GetRecentActivityAsync(int count);
    Task<SalesTransaction> RecordSaleAsync(SalesTransaction transaction);
}

public class InventoryService : IInventoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryService> _logger;
    private readonly string _baseUrl;

    public InventoryService(HttpClient httpClient, IConfiguration configuration, ILogger<InventoryService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://api.inventorypredictor.com/api/v1";
        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    public async Task<List<InventoryItem>> GetAllInventoryAsync(string? location = null, string? category = null)
    {
        try
        {
            var query = BuildQueryString(new Dictionary<string, string?>
            {
                { "location", location },
                { "category", category }
            });

            var response = await _httpClient.GetAsync($"inventory{query}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<InventoryItem>>>();
            return apiResponse?.Data ?? new List<InventoryItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching inventory items");
            throw;
        }
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"inventory/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<InventoryItem>>();
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching inventory item {Id}", id);
            return null;
        }
    }

    public async Task<InventoryItem> UpdateInventoryAsync(Guid id, InventoryItem item)
    {
        try
        {
            var json = JsonSerializer.Serialize(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"inventory/{id}", content);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<InventoryItem>>();
            return apiResponse?.Data ?? item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item {Id}", id);
            throw;
        }
    }

    public async Task<List<StockAlert>> GetActiveAlertsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("alerts?resolved=false");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<StockAlert>>>();
            return apiResponse?.Data ?? new List<StockAlert>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active alerts");
            return new List<StockAlert>();
        }
    }

    public async Task<InventoryAnalytics> GetAnalyticsAsync(string? location = null)
    {
        try
        {
            var query = location != null ? $"?location={Uri.EscapeDataString(location)}" : "";
            var response = await _httpClient.GetAsync($"analytics/inventory{query}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<InventoryAnalytics>>();
            return apiResponse?.Data ?? new InventoryAnalytics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching analytics");
            return new InventoryAnalytics();
        }
    }

    public async Task<SalesSummary> GetSalesSummaryAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var query = $"?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync($"sales/summary{query}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<SalesSummary>>();
            return apiResponse?.Data ?? new SalesSummary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sales summary");
            return new SalesSummary();
        }
    }

    public async Task<List<SalesTrendPoint>> GetSalesTrendsAsync(int days)
    {
        try
        {
            var response = await _httpClient.GetAsync($"analytics/sales-trends?period=daily&days={days}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesTrendPoint>>>();
            return apiResponse?.Data ?? new List<SalesTrendPoint>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sales trends");
            return new List<SalesTrendPoint>();
        }
    }

    public async Task<List<CategoryData>> GetInventoryByCategoryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("analytics/inventory-by-category");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryData>>>();
            return apiResponse?.Data ?? new List<CategoryData>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category data");
            return new List<CategoryData>();
        }
    }

    public async Task<List<ActivityItem>> GetRecentActivityAsync(int count)
    {
        try
        {
            var response = await _httpClient.GetAsync($"analytics/recent-activity?count={count}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<ActivityItem>>>();
            return apiResponse?.Data ?? new List<ActivityItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent activity");
            return new List<ActivityItem>();
        }
    }

    public async Task<SalesTransaction> RecordSaleAsync(SalesTransaction transaction)
    {
        try
        {
            var json = JsonSerializer.Serialize(transaction);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("sales", content);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<SalesTransaction>>();
            return apiResponse?.Data ?? transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording sale");
            throw;
        }
    }

    private string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var nonNullParams = parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value));
        if (!nonNullParams.Any())
            return "";

        var query = string.Join("&", nonNullParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));
        return $"?{query}";
    }
}

// Services/PredictionService.cs
public interface IPredictionService
{
    Task<List<PredictionResult>> GetStockOutPredictionsAsync(int daysAhead);
    Task<List<ReorderRecommendation>> GetReorderRecommendationsAsync();
    Task<List<PredictionResult>> GetDemandForecastsAsync();
    Task<PredictionResult?> GetDemandForecastAsync(Guid productId, int days);
    Task<List<TrendInsight>> GetTrendInsightsAsync();
    Task RunPredictionModelAsync(List<Guid> productIds, int forecastDays);
}

public class PredictionService : IPredictionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PredictionService> _logger;
    private readonly string _baseUrl;

    public PredictionService(HttpClient httpClient, IConfiguration configuration, ILogger<PredictionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://api.inventorypredictor.com/api/v1";
        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    public async Task<List<PredictionResult>> GetStockOutPredictionsAsync(int daysAhead)
    {
        try
        {
            var response = await _httpClient.GetAsync($"predictions/stockout?daysAhead={daysAhead}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<StockOutPredictionsResponse>>();
            return apiResponse?.Data?.Predictions ?? new List<PredictionResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock-out predictions");
            return new List<PredictionResult>();
        }
    }

    public async Task<List<ReorderRecommendation>> GetReorderRecommendationsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("predictions/reorder-recommendations");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ReorderRecommendationsResponse>>();
            return apiResponse?.Data?.Recommendations ?? new List<ReorderRecommendation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reorder recommendations");
            return new List<ReorderRecommendation>();
        }
    }

    public async Task<List<PredictionResult>> GetDemandForecastsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("predictions/demand-forecasts");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<PredictionResult>>>();
            return apiResponse?.Data ?? new List<PredictionResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching demand forecasts");
            return new List<PredictionResult>();
        }
    }

    public async Task<PredictionResult?> GetDemandForecastAsync(Guid productId, int days)
    {
        try
        {
            var response = await _httpClient.GetAsync($"predictions/demand/{productId}?days={days}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PredictionResult>>();
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching demand forecast for product {ProductId}", productId);
            return null;
        }
    }

    public async Task<List<TrendInsight>> GetTrendInsightsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("analytics/trend-insights");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<TrendInsight>>>();
            return apiResponse?.Data ?? new List<TrendInsight>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trend insights");
            return new List<TrendInsight>();
        }
    }

    public async Task RunPredictionModelAsync(List<Guid> productIds, int forecastDays)
    {
        try
        {
            var request = new RunPredictionRequest
            {
                ProductIds = productIds,
                ForecastDays = forecastDays,
                IncludeSeasonality = true
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("predictions/run", content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running prediction model");
            throw;
        }
    }
}

// Services/SignalRService.cs
using Microsoft.AspNetCore.SignalR.Client;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SignalRService> _logger;

    public event Action<InventoryItem>? OnStockLevelChanged;
    public event Action<StockAlert>? OnNewAlert;
    public event Action<SalesTransaction>? OnSaleRecorded;
    public event Action<PredictionResult>? OnPredictionUpdated;

    public SignalRService(IConfiguration configuration, ILogger<SignalRService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        var hubUrl = _configuration["ApiSettings:SignalRHub"] ?? "https://api.inventorypredictor.com/hubs/inventory";
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Register event handlers
        _hubConnection.On<InventoryItem>("StockLevelChanged", (item) =>
        {
            _logger.LogInformation("Stock level changed for product {ProductId}", item.Id);
            OnStockLevelChanged?.Invoke(item);
        });

        _hubConnection.On<StockAlert>("NewAlert", (alert) =>
        {
            _logger.LogInformation("New alert: {AlertType} for product {ProductId}", alert.Type, alert.ProductId);
            OnNewAlert?.Invoke(alert);
        });

        _hubConnection.On<SalesTransaction>("SaleRecorded", (transaction) =>
        {
            _logger.LogInformation("Sale recorded: {ProductName} - {Quantity} units", transaction.ProductName, transaction.Quantity);
            OnSaleRecorded?.Invoke(transaction);
        });

        _hubConnection.On<PredictionResult>("PredictionUpdated", (prediction) =>
        {
            _logger.LogInformation("Prediction updated for product {ProductId}", prediction.ProductId);
            OnPredictionUpdated?.Invoke(prediction);
        });

        try
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to SignalR hub");
        }
    }

    public async Task SubscribeToProduct(Guid productId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SubscribeToProduct", productId);
        }
    }

    public async Task SubscribeToLocation(string location)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SubscribeToLocation", location);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}

// Models/ApiResponse.cs
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Details { get; set; }
}

// Additional response models
public class StockOutPredictionsResponse
{
    public List<PredictionResult> Predictions { get; set; } = new();
}

public class ReorderRecommendationsResponse
{
    public List<ReorderRecommendation> Recommendations { get; set; } = new();
}

public class RunPredictionRequest
{
    public List<Guid> ProductIds { get; set; } = new();
    public int ForecastDays { get; set; }
    public bool IncludeSeasonality { get; set; }
}
