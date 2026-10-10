namespace StockMind.Identity.Domain.Entities;

public sealed class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "Usuario";
    public bool Activo { get; set; } = true;
}

public readonly record struct Email
{
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new ArgumentException("El correo no es valido.", nameof(value));
        Value = value.Trim().ToLowerInvariant();
    }
    public string Value { get; }
    public override string ToString() => Value;
}

