using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Auth;
using ResQ.API.Services.Auth;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // POST api/auth/register/consumer
    [HttpPost("register/consumer")]
    public async Task<IActionResult> RegisterConsumer(
        [FromBody] RegisterConsumerRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await authService.RegisterConsumerAsync(request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST api/auth/register/merchant
    [HttpPost("register/merchant")]
    public async Task<IActionResult> RegisterMerchant(
        [FromBody] RegisterMerchantRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await authService.RegisterMerchantAsync(request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await authService.LoginAsync(request, ct);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await authService.RefreshTokenAsync(request.RefreshToken, ct);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        await authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }
}
