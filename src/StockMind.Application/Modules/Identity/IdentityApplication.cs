using StockMind.Identity.Domain.Entities;

namespace StockMind.Identity.Application;

public sealed record LoginRequest(string Email, string Password);
public sealed record AuthUser(string Id, string Name, string Email, IReadOnlyCollection<string> Roles);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, AuthUser User);

public interface ILoginUseCase
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
public interface IJwtTokenService
{
    AuthResponse CreateToken(User user);
}
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public sealed class LoginUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokens) : ILoginUseCase
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedAccessException("Credenciales invalidas.");

        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.Activo || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales invalidas.");

        return tokens.CreateToken(user);
    }
}


