namespace StockMind.Sales.Domain.Entities;

public sealed class Sale
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public List<SaleDetail> Detalles { get; set; } = [];
    public decimal Total => Detalles.Sum(x => x.Subtotal);

    public static Sale Create(IEnumerable<SaleDetail> details)
    {
        var materialized = details?.ToList()
            ?? throw new ArgumentNullException(nameof(details));
        if (materialized.Count == 0)
            throw new ArgumentException("La venta debe contener al menos un detalle.", nameof(details));

        return new Sale { Detalles = materialized };
    }
}

public sealed class SaleDetail
{
    public string ProductoId { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;

    public static SaleDetail Create(string productoId, int cantidad, decimal precioUnitario)
    {
        if (string.IsNullOrWhiteSpace(productoId))
            throw new ArgumentException("El producto es obligatorio.", nameof(productoId));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
        if (precioUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitario), "El precio unitario no puede ser negativo.");

        return new SaleDetail
        {
            ProductoId = productoId.Trim(),
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario
        };
    }
}

