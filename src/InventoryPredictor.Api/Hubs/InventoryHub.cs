
// Hubs/InventoryHub.cs
namespace InventoryPredictor.Api.Hubs;

using Microsoft.AspNetCore.SignalR;
using InventoryPredictor.Shared.Models;

public class InventoryHub : Hub
{
    private readonly ILogger<InventoryHub> _logger;

    public InventoryHub(ILogger<InventoryHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToProduct(Guid productId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"product_{productId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to product {ProductId}", 
            Context.ConnectionId, productId);
    }

    public async Task UnsubscribeFromProduct(Guid productId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"product_{productId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from product {ProductId}", 
            Context.ConnectionId, productId);
    }

    public async Task SubscribeToLocation(string location)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"location_{location}");
        _logger.LogInformation("Client {ConnectionId} subscribed to location {Location}", 
            Context.ConnectionId, location);
    }

    public async Task UnsubscribeFromLocation(string location)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"location_{location}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from location {Location}", 
            Context.ConnectionId, location);
    }

    // Methods to send updates to clients
    public async Task NotifyStockLevelChanged(InventoryItem item)
    {
        await Clients.Group($"product_{item.Id}").SendAsync("StockLevelChanged", item);
        await Clients.Group($"location_{item.Location}").SendAsync("StockLevelChanged", item);
    }

    public async Task NotifyNewAlert(StockAlert alert)
    {
        await Clients.All.SendAsync("NewAlert", alert);
    }

    public async Task NotifySaleRecorded(SalesTransaction transaction)
    {
        await Clients.Group($"product_{transaction.ProductId}").SendAsync("SaleRecorded", transaction);
        await Clients.Group($"location_{transaction.Location}").SendAsync("SaleRecorded", transaction);
    }

    public async Task NotifyPredictionUpdated(PredictionResult prediction)
    {
        await Clients.Group($"product_{prediction.ProductId}").SendAsync("PredictionUpdated", prediction);
    }
}