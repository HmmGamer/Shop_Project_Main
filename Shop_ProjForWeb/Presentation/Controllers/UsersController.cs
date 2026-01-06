namespace Shop_ProjForWeb.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;

/// <summary>
/// Manages user accounts and VIP status
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService, IValidationService validationService) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly IValidationService _validationService = validationService;

    /// <summary>
    /// Retrieves all users with pagination and sorting
    /// </summary>
    /// <param name="request">Pagination parameters (page, pageSize, sortBy, sortDescending)</param>
    /// <returns>Paginated list of users</returns>
    /// <response code="200">Returns the paginated list of users</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetAllUsers([FromQuery] PaginatedRequest request)
    {
        var users = await _userService.GetAllUsersAsync();
        
        // Apply sorting
        var sortedUsers = request.SortBy.ToLower() switch
        {
            "fullname" => request.SortDescending ? users.OrderByDescending(u => u.FullName) : users.OrderBy(u => u.FullName),
            "isvip" => request.SortDescending ? users.OrderByDescending(u => u.IsVip) : users.OrderBy(u => u.IsVip),
            "createdat" => request.SortDescending ? users.OrderByDescending(u => u.CreatedAt) : users.OrderBy(u => u.CreatedAt),
            _ => request.SortDescending ? users.OrderByDescending(u => u.Id) : users.OrderBy(u => u.Id)
        };

        // Apply pagination
        var totalCount = sortedUsers.Count();
        var pagedUsers = sortedUsers
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = new PaginatedResponse<UserDto>
        {
            Items = pagedUsers,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific user by ID
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <returns>User details</returns>
    /// <response code="200">Returns the user details</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await _userService.GetUserAsync(id);
        return Ok(user);
    }

    /// <summary>
    /// Creates a new user account
    /// </summary>
    /// <param name="dto">User creation details (FullName, PhoneNumber)</param>
    /// <returns>The created user</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid input or validation failed</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        // Use ValidationService for consistent validation
        var validationResult = await _validationService.ValidateBusinessRulesAsync(dto, "create");
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage });
            return BadRequest(new { error = "Validation failed", validationErrors = errors });
        }

        var user = await _userService.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Updates an existing user's information
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <param name="dto">Updated user details (FullName, PhoneNumber)</param>
    /// <returns>No content on success</returns>
    /// <response code="204">User updated successfully</response>
    /// <response code="400">Invalid input or validation failed</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        // Validate if FullName is provided
        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            var nameValidation = _validationService.ValidateRequired(dto.FullName, "FullName");
            if (!nameValidation.IsValid)
            {
                var errors = nameValidation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage });
                return BadRequest(new { error = "Validation failed", validationErrors = errors });
            }

            if (dto.FullName.Length < 2 || dto.FullName.Length > 100)
            {
                return BadRequest(new { error = "Full name must be between 2 and 100 characters" });
            }
        }

        await _userService.UpdateUserAsync(id, dto);
        return NoContent();
    }

    /// <summary>
    /// Soft deletes a user (marks as deleted without removing from database)
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <returns>No content on success</returns>
    /// <response code="204">User deleted successfully</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }
}
