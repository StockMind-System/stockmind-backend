using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using StockMind.Identity.Application;
using StockMind.Identity.Domain.Entities;

namespace StockMind.Identity.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "StockMind";
    public string Audience { get; set; } = "StockMind.Client";
    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
    public bool Verify(string password, string encodedHash)
    {
        var parts = encodedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

internal sealed class JwtTokenService(IOptions<JwtSettings> options) : IJwtTokenService
{
    public AuthResponse CreateToken(User user)
    {
        var settings = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes);
        var role = user.Rol.Trim().ToUpperInvariant();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, role)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer, settings.Audience, claims,
            expires: expires, signingCredentials: credentials);
        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires,
            new AuthUser(user.Id, user.Nombre, user.Email, [role]));
    }
}

internal sealed class MongoUserRepository(IMongoDatabase database) : IUserRepository
{
    private IMongoCollection<User> Users => database.GetCollection<User>("usuarios");

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Users.Find(x => x.Email == email).FirstOrDefaultAsync(cancellationToken);
}

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(JwtSettings.SectionName);
        var settings = section.Get<JwtSettings>() ?? new JwtSettings();
        services.Configure<JwtSettings>(section);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = settings.Issuer,
                ValidAudience = settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                ClockSkew = TimeSpan.FromMinutes(1)
            });
        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        return services;
    }
}

