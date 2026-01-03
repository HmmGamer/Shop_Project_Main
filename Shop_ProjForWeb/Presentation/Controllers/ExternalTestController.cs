namespace Shop_ProjForWeb.Presentation.Controllers;

using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.Services;

/// <summary>
/// External API integration testing (Agify API for age prediction)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExternalTestController : ControllerBase
{
    private readonly AgifyService _agifyService;

    public ExternalTestController(AgifyService agifyService)
    {
        _agifyService = agifyService;
    }

    /// <summary>
    /// Predicts age based on a given name using Agify API
    /// </summary>
    /// <param name="name">The name to predict age for</param>
    /// <returns>Predicted age for the given name</returns>
    /// <response code="200">Returns the predicted age</response>
    [HttpGet("age/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAge(string name)
    {
        var age = await _agifyService.GetPredictedAgeAsync(name);
        return Ok(new { name, age });
    }
}
