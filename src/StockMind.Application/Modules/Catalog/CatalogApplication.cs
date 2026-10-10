using StockMind.Catalog.Domain.Entities;

namespace StockMind.Catalog.Application;

public sealed record ProductDto(
    string Id, string Sku, string Nombre, string? CategoriaId,
    decimal Precio, int StockActual, int StockMinimo, bool Activo);

public sealed record SaveProductRequest(
    string Sku, string Nombre, string? CategoriaId,
    decimal Precio, int StockActual, int StockMinimo);

public interface IProductRepository
{
    Task<IReadOnlyCollection<Product>> ListAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task CreateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IProductUseCases
{
    Task<IReadOnlyCollection<ProductDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(SaveProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class ProductUseCases(IProductRepository repository) : IProductUseCases
{
    public async Task<IReadOnlyCollection<ProductDto>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).Select(ToDto).ToArray();

    public async Task<ProductDto?> GetAsync(string id, CancellationToken ct = default) =>
        await repository.GetAsync(id, ct) is { } product ? ToDto(product) : null;

    public async Task<ProductDto> CreateAsync(SaveProductRequest request, CancellationToken ct = default)
    {
        var product = Product.Create(
            request.Sku, request.Nombre, request.CategoriaId,
            request.Precio, request.StockActual, request.StockMinimo);
        await repository.CreateAsync(product, ct);
        return ToDto(product);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        if (await repository.GetAsync(id, ct) is null) return false;
        await repository.DeleteAsync(id, ct);
        return true;
    }

    private static ProductDto ToDto(Product value) => new(
        value.Id, value.Sku, value.Nombre, value.CategoriaId,
        value.Precio, value.StockActual, value.StockMinimo, value.Activo);
}


