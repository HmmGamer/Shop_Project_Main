namespace Shop_ProjForWeb.Presentation.Controllers;

using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Exceptions;

/// <summary>
/// Manages product inventory levels and stock tracking
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController(
    IInventoryRepository inventoryRepository,
    IInventoryService inventoryService) : ControllerBase
{
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;
    private readonly IInventoryService _inventoryService = inventoryService;

    /// <summary>
    /// Retrieves inventory status for a specific product
    /// </summary>
    /// <param name="productId">The unique identifier of the product</param>
    /// <returns>Inventory status including quantity and low stock flag</returns>
    /// <response code="200">Returns the inventory status</response>
    /// <response code="404">Product not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{productId}")]
    [ProducesResponseType(typeof(InventoryStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InventoryStatusDto>> GetInventoryStatus(Guid productId)
    {
        try
        {
            var status = await _inventoryService.GetInventoryStatusAsync(productId);
            return Ok(status);
        }
        catch (ProductNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving inventory status", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves inventory levels for all products
    /// </summary>
    /// <returns>List of all inventory items</returns>
    /// <response code="200">Returns the list of all inventory items</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<InventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<InventoryDto>>> GetAllInventory()
    {
        try
        {
            var items = await _inventoryRepository.GetAllAsync();
            var dtos = items.Select(i => new InventoryDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "Unknown",
                Quantity = i.Quantity,
                LowStockFlag = i.LowStockFlag,
                LastUpdatedAt = i.LastUpdatedAt
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving inventory", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves products with low stock levels (below threshold)
    /// </summary>
    /// <returns>List of low stock items requiring restocking</returns>
    /// <response code="200">Returns the list of low stock items</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(List<InventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<InventoryDto>>> GetLowStockItems()
    {
        try
        {
            var items = await _inventoryService.GetLowStockItemsAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving low stock items", details = ex.Message });
        }
    }

    /// <summary>
    /// Creates inventory record for a new product
    /// </summary>
    /// <param name="dto">Inventory creation details (ProductId, Quantity)</param>
    /// <returns>The created inventory record</returns>
    /// <response code="201">Inventory created successfully</response>
    /// <response code="400">Invalid input (negative quantity or empty product ID)</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InventoryDto>> CreateInventory([FromBody] CreateInventoryDto dto)
    {
        try
        {
            if (dto.ProductId == Guid.Empty)
            {
                return BadRequest(new { error = "Product ID is required" });
            }

            if (dto.Quantity < 0)
            {
                return BadRequest(new { error = "Quantity cannot be negative" });
            }

            var inventory = new Shop_ProjForWeb.Core.Domain.Entities.Inventory
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                LowStockThreshold = 10, // Default threshold
                LowStockFlag = dto.Quantity < 10,
                LastUpdatedAt = DateTime.UtcNow
            };

            await _inventoryRepository.AddAsync(inventory);

            return CreatedAtAction(nameof(GetAllInventory), new InventoryDto
            {
                ProductId = inventory.ProductId,
                ProductName = "Unknown",
                Quantity = inventory.Quantity,
                LowStockFlag = inventory.LowStockFlag,
                LastUpdatedAt = inventory.LastUpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating inventory", details = ex.Message });
        }
    }

    /// <summary>
    /// Updates inventory quantity for a product
    /// </summary>
    /// <param name="productId">The unique identifier of the product</param>
    /// <param name="dto">Updated inventory details (Quantity)</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Inventory updated successfully</response>
    /// <response code="400">Invalid input (negative quantity)</response>
    /// <response code="404">Inventory not found for this product</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateInventory(Guid productId, [FromBody] UpdateInventoryDto dto)
    {
        try
        {
            if (dto.Quantity < 0)
            {
                return BadRequest(new { error = "Quantity cannot be negative" });
            }

            var inventory = await _inventoryRepository.GetByProductIdAsync(productId);
            if (inventory == null)
            {
                return NotFound(new { error = "Inventory not found for this product" });
            }

            inventory.Quantity = dto.Quantity;
            inventory.LowStockFlag = dto.Quantity < inventory.LowStockThreshold;
            inventory.LastUpdatedAt = DateTime.UtcNow;

            await _inventoryRepository.UpdateAsync(inventory);

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating inventory", details = ex.Message });
        }
    }
}
