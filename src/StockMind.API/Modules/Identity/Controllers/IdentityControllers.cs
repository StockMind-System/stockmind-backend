using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockMind.Identity.Application;

namespace StockMind.Identity.Api.Controllers;

[ApiController, Route("api/auth"), AllowAnonymous]
public sealed class AuthController(ILoginUseCase login) : ControllerBase
{
    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) => login.LoginAsync(request, ct);
}

[ApiController, Route("api/usuarios"), Authorize]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Current() => Ok(new
    {
        Id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
        Email = User.FindFirstValue(ClaimTypes.Email)
    });
}

