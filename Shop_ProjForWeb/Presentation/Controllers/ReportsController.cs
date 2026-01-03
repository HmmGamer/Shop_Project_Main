using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.Services;

namespace Shop_ProjForWeb.Presentation.Controllers;

/// <summary>
/// Provides business intelligence and analytics reports
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController(ReportingService reportingService) : ControllerBase
{
    private readonly ReportingService _reportingService = reportingService;

    /// <summary>
    /// Retrieves sales summary report with optional date filtering
    /// </summary>
    /// <param name="startDate">Optional start date for filtering (format: yyyy-MM-dd)</param>
    /// <param name="endDate">Optional end date for filtering (format: yyyy-MM-dd)</param>
    /// <returns>Sales summary including total revenue and order count</returns>
    /// <response code="200">Returns the sales summary report</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("sales")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSalesSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var report = await _reportingService.GetSalesSummaryAsync(startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving sales summary", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves comprehensive inventory report for all products
    /// </summary>
    /// <returns>Inventory report with stock levels and low stock alerts</returns>
    /// <response code="200">Returns the inventory report</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("inventory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInventoryReport()
    {
        try
        {
            var report = await _reportingService.GetInventoryReportAsync();
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving inventory report", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves spending report for a specific user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>User spending report including total spent and order history</returns>
    /// <response code="200">Returns the user spending report</response>
    /// <response code="400">Invalid user ID</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("user/{userId}/spending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserSpendingReport(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "User ID is required" });
            }

            var report = await _reportingService.GetUserSpendingReportAsync(userId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving user spending report", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves top-selling products ranked by quantity sold
    /// </summary>
    /// <param name="limit">Number of top products to return (default: 10)</param>
    /// <returns>List of top products with sales statistics</returns>
    /// <response code="200">Returns the list of top products</response>
    /// <response code="400">Invalid limit (must be greater than 0)</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("top-products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
    {
        try
        {
            if (limit <= 0)
            {
                return BadRequest(new { error = "Limit must be greater than 0" });
            }

            var products = await _reportingService.GetTopProductsAsync(limit);
            return Ok(products);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving top products", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves spending report for all users
    /// </summary>
    /// <returns>List of all users with their spending statistics</returns>
    /// <response code="200">Returns the spending report for all users</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("users/spending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllUsersSpendingReport()
    {
        try
        {
            var users = await _reportingService.GetAllUsersSpendingReportAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving users spending report", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves order status distribution statistics
    /// </summary>
    /// <returns>Distribution of orders by status (Pending, Paid, Cancelled)</returns>
    /// <response code="200">Returns the order status distribution</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("order-status-distribution")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderStatusDistribution()
    {
        try
        {
            var distribution = await _reportingService.GetOrderStatusDistributionAsync();
            return Ok(distribution);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving order status distribution", details = ex.Message });
        }
    }
}
