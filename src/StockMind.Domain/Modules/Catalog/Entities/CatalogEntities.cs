namespace StockMind.Catalog.Domain.Entities;

public sealed class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? CategoriaId { get; set; }
    public decimal Precio { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public bool Activo { get; set; } = true;

    public static Product Create(
        string sku, string nombre, string? categoriaId,
        decimal precio, int stockActual, int stockMinimo)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("El SKU es obligatorio.", nameof(sku));
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (precio < 0)
            throw new ArgumentOutOfRangeException(nameof(precio), "El precio no puede ser negativo.");
        if (stockActual < 0)
            throw new ArgumentOutOfRangeException(nameof(stockActual), "El stock actual no puede ser negativo.");
        if (stockMinimo < 0)
            throw new ArgumentOutOfRangeException(nameof(stockMinimo), "El stock minimo no puede ser negativo.");

        return new Product
        {
            Sku = sku.Trim().ToUpperInvariant(),
            Nombre = nombre.Trim(),
            CategoriaId = string.IsNullOrWhiteSpace(categoriaId) ? null : categoriaId.Trim(),
            Precio = precio,
            StockActual = stockActual,
            StockMinimo = stockMinimo
        };
    }
}

public sealed class Category
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

