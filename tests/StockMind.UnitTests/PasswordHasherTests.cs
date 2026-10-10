using StockMind.Identity.Infrastructure.Security;
using Xunit;

namespace StockMind.UnitTests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_Y_Verify_ProtegenLaContrasena()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("StockMind-123");

        Assert.NotEqual("StockMind-123", hash);
        Assert.True(hasher.Verify("StockMind-123", hash));
        Assert.False(hasher.Verify("incorrecta", hash));
    }
}
