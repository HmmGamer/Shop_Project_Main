using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.Services;
using Shop_ProjForWeb.Core.Application.DTOs;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Shop_ProjForWeb.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<IActionResult> Token([FromBody] LoginRequestDto request)
    {
        // input validation
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new GenericResponseDto<string> { Success = false, Message = "Email and password are required" });

        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == request.Email);
        if (user == null)
            return Unauthorized(new GenericResponseDto<string> { Success = false, Message = "Invalid credentials" });

        // Verify password using PBKDF2 check
        if (string.IsNullOrEmpty(user.PasswordHash) || !PasswordHasher.VerifyHashedPassword(user.PasswordHash, request.Password))
            return Unauthorized(new GenericResponseDto<string> { Success = false, Message = "Invalid credentials" });

        var roles = new List<string>();
        if (user.VipTier > 0) roles.Add("Vip");
        roles.Add("User");

        var token = _jwtService.GenerateToken(user, roles);
        return Ok(new GenericResponseDto<object> 
        { 
            Success = true, 
            Data = new 
            { 
                Token = token,
                User = new 
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsVip = user.IsVip,
                    VipTier = user.VipTier,
                    TotalSpending = user.TotalSpending
                }
            } 
        });
    }
}
