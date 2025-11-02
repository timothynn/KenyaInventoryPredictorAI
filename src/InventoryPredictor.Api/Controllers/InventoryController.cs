// Controllers/InventoryController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryPredictor.Api.Data;
using InventoryPredictor.Api.Services;
using InventoryPredictor.Shared.Models;
using InventoryPredictor.Shared.DTOs;

namespace InventoryPredictor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEventHubService _eventHubService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        AppDbContext context,
        IEventHubService eventHubService,
        ILogger<InventoryController> logger)
    {
        _context = context;
        _eventHubService = eventHubService;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory items with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InventoryItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? location = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.InventoryItems.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(i => i.Location == location);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(i => i.Category == category);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StockStatus>(status, out var statusEnum))
                query = query.Where(i => i.Status == statusEnum);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderBy(i => i.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<InventoryItem>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(new ApiResponse<PagedResult<InventoryItem>>
            {
                Success = true,
                Data = result,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching inventory items");
            return StatusCode(500, new ApiResponse<PagedResult<InventoryItem>>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching inventory items"
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get single inventory item by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItem>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _context.InventoryItems.FindAsync(id);

            if (item == null)
            {
                return NotFound(new ApiResponse<InventoryItem>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "NOT_FOUND",
                        Message = $"Inventory item with ID {id} not found"
                    },
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new ApiResponse<InventoryItem>
            {
                Success = true,
                Data = item,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching inventory item {Id}", id);
            return StatusCode(500, new ApiResponse<InventoryItem>
            {
                Success = false,
                Error = new ApiError { Code = "INTERNAL_ERROR", Message = "An error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Create new inventory item
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InventoryItem>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] InventoryItem item)
    {
        try
        {
            item.Id = Guid.NewGuid();
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            item.Status = CalculateStockStatus(item);

            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();

            // Publish event to Event Hub
            await _eventHubService.PublishInventoryEventAsync(new
            {
                EventType = "InventoryItemCreated",
                Item = item,
                Timestamp = DateTime.UtcNow
            });

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<InventoryItem>
            {
                Success = true,
                Data = item,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory item");
            return StatusCode(500, new ApiResponse<InventoryItem>
            {
                Success = false,
                Error = new ApiError { Code = "INTERNAL_ERROR", Message = "An error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Update existing inventory item
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] InventoryItem item)
    {
        try
        {
            var existingItem = await _context.InventoryItems.FindAsync(id);
            if (existingItem == null)
            {
                return NotFound(new ApiResponse<InventoryItem>
                {
                    Success = false,
                    Error = new ApiError { Code = "NOT_FOUND", Message = "Item not found" },
                    Timestamp = DateTime.UtcNow
                });
            }

            // Update properties
            existingItem.ProductName = item.ProductName;
            existingItem.Category = item.Category;
            existingItem.CurrentStock = item.CurrentStock;
            existingItem.MinimumStock = item.MinimumStock;
            existingItem.MaximumStock = item.MaximumStock;
            existingItem.ReorderPoint = item.ReorderPoint;
            existingItem.OptimalOrderQuantity = item.OptimalOrderQuantity;
            existingItem.UnitPrice = item.UnitPrice;
            existingItem.Location = item.Location;
            existingItem.Supplier = item.Supplier;
            existingItem.LeadTimeDays = item.LeadTimeDays;
            existingItem.UpdatedAt = DateTime.UtcNow;
            existingItem.Status = CalculateStockStatus(existingItem);

            await _context.SaveChangesAsync();

            // Publish event
            await _eventHubService.PublishInventoryEventAsync(new
            {
                EventType = "InventoryItemUpdated",
                Item = existingItem,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new ApiResponse<InventoryItem>
            {
                Success = true,
                Data = existingItem,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item {Id}", id);
            return StatusCode(500, new ApiResponse<InventoryItem>
            {
                Success = false,
                Error = new ApiError { Code = "INTERNAL_ERROR", Message = "An error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Delete inventory item
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item {Id}", id);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Record stock movement (sale, purchase, adjustment)
    /// </summary>
    [HttpPost("movement")]
    [ProducesResponseType(typeof(ApiResponse<InventoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordMovement([FromBody] StockMovementRequest request)
    {
        try
        {
            var item = await _context.InventoryItems.FindAsync(request.ProductId);
            if (item == null)
            {
                return NotFound(new ApiResponse<InventoryItem>
                {
                    Success = false,
                    Error = new ApiError { Code = "NOT_FOUND", Message = "Product not found" },
                    Timestamp = DateTime.UtcNow
                });
            }

            // Update stock based on movement type
            switch (request.Type)
            {
                case "Sale":
                case "Damaged":
                case "Expired":
                    item.CurrentStock -= request.Quantity;
                    break;
                case "Purchase":
                case "Return":
                case "Adjustment":
                    item.CurrentStock += request.Quantity;
                    break;
                case "Transfer":
                    item.CurrentStock -= request.Quantity;
                    break;
            }

            item.UpdatedAt = DateTime.UtcNow;
            item.Status = CalculateStockStatus(item);

            // Calculate days of stock remaining
            var avgDailySales = await GetAverageDailySales(item.Id);
            if (avgDailySales > 0)
            {
                item.DaysOfStockRemaining = item.CurrentStock / avgDailySales;
            }

            await _context.SaveChangesAsync();

            // Check if alert should be created
            await CheckAndCreateAlert(item);

            // Publish event
            await _eventHubService.PublishInventoryEventAsync(new
            {
                EventType = "StockMovement",
                ProductId = item.Id,
                MovementType = request.Type,
                Quantity = request.Quantity,
                NewStock = item.CurrentStock,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new ApiResponse<InventoryItem>
            {
                Success = true,
                Data = item,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording stock movement");
            return StatusCode(500, new ApiResponse<InventoryItem>
            {
                Success = false,
                Error = new ApiError { Code = "INTERNAL_ERROR", Message = "An error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get low stock items
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock()
    {
        try
        {
            var items = await _context.InventoryItems
                .Where(i => i.CurrentStock <= i.MinimumStock)
                .OrderBy(i => i.CurrentStock / i.MinimumStock)
                .ToListAsync();

            return Ok(new ApiResponse<List<InventoryItem>>
            {
                Success = true,
                Data = items,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching low stock items");
            return StatusCode(500, new ApiResponse<List<InventoryItem>>
            {
                Success = false,
                Error = new ApiError { Code = "INTERNAL_ERROR", Message = "An error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get inventory by location
    /// </summary>
    [HttpGet("by-location/{location}")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLocation(string location)
    {
        try
        {
            var items = await _context.InventoryItems
                .Where(i => i.Location == location)
                .OrderBy(i => i.ProductName)
                .ToListAsync();

            return Ok(new ApiResponse<List<InventoryItem>>
            {
                Success = true,
                Data = items,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching inventory by location {Location}", location);
            return StatusCode(500, new ApiResponse<List<InventoryItem>>
            {
                Success = false,
                Error = new ApiError { Code = "INTERNAL_ERROR", Message = "An error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    // Helper methods
    private StockStatus CalculateStockStatus(InventoryItem item)
    {
        if (item.CurrentStock == 0)
            return StockStatus.OutOfStock;

        var stockRatio = item.CurrentStock / item.MinimumStock;

        return stockRatio switch
        {
            < 0.5m => StockStatus.CriticallyLow,
            < 1.0m => StockStatus.Low,
            >= 1.0m and < 1.5m => StockStatus.Optimal,
            >= 1.5m and < 2.0m => StockStatus.High,
            >= 2.0m => StockStatus.Overstocked,
            _ => StockStatus.Optimal // Fallback for values not matching any previous pattern
        };
    }

    private async Task<decimal> GetAverageDailySales(Guid productId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var totalSales = await _context.SalesTransactions
            .Where(t => t.ProductId == productId && t.TransactionDate >= thirtyDaysAgo)
            .SumAsync(t => t.Quantity);

        return totalSales / 30m;
    }

    private async Task CheckAndCreateAlert(InventoryItem item)
    {
        // Check if alert already exists for this product
        var existingAlert = await _context.StockAlerts
            .Where(a => a.ProductId == item.Id && !a.IsResolved)
            .FirstOrDefaultAsync();

        if (item.Status == StockStatus.CriticallyLow || item.Status == StockStatus.OutOfStock)
        {
            if (existingAlert == null)
            {
                var alert = new StockAlert
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.Id,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    Type = item.Status == StockStatus.OutOfStock ? AlertType.StockOut : AlertType.LowStock,
                    Severity = item.Status == StockStatus.OutOfStock ? AlertSeverity.Critical : AlertSeverity.High,
                    Message = $"{item.ProductName} is {(item.Status == StockStatus.OutOfStock ? "out of stock" : "critically low")} at {item.Location}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsResolved = false,
                    Location = item.Location,
                    CurrentStock = item.CurrentStock,
                    ThresholdValue = item.MinimumStock,
                    RecommendedAction = $"Reorder {item.OptimalOrderQuantity} {item.Unit} immediately"
                };

                _context.StockAlerts.Add(alert);
                await _context.SaveChangesAsync();
            }
        }
        else if (existingAlert != null)
        {
            // Resolve alert if stock is back to normal
            existingAlert.IsResolved = true;
            await _context.SaveChangesAsync();
        }
    }
}
