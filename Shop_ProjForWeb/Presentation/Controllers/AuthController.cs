using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.Services;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;

namespace Shop_ProjForWeb.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly IUserRepository _userRepository;

    public AuthController(JwtService jwtService, IUserRepository userRepository)
    {
        _jwtService = jwtService;
        _userRepository = userRepository;
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] LoginRequestDto request)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == request.Email);
        if (user == null)
            return Unauthorized(new GenericResponseDto<string> { Success = false, Message = "Invalid credentials" });

        // NOTE: No password checks in seed/demo. In real app verify hashed password.

        var roles = new List<string>();
        if (user.VipTier > 0) roles.Add("Vip");
        roles.Add("User");

        var token = _jwtService.GenerateToken(user, roles);
        return Ok(new GenericResponseDto<object> { Success = true, Data = new { Token = token } });
    }
}
