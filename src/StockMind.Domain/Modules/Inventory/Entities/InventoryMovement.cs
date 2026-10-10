namespace StockMind.Inventory.Domain.Entities;

public enum MovementType { Entrada, Salida, Ajuste }

public sealed class InventoryMovement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductoId { get; set; } = string.Empty;
    public MovementType Tipo { get; set; }
    public int Cantidad { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    public static InventoryMovement Create(
        string productoId, MovementType tipo, int cantidad, string? motivo)
    {
        if (string.IsNullOrWhiteSpace(productoId))
            throw new ArgumentException("El producto es obligatorio.", nameof(productoId));
        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo), "El tipo de movimiento no es valido.");
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");

        return new InventoryMovement
        {
            ProductoId = productoId.Trim(),
            Tipo = tipo,
            Cantidad = cantidad,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim()
        };
    }
}

