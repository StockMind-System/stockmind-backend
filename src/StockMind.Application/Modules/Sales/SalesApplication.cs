using StockMind.Sales.Domain.Entities;

namespace StockMind.Sales.Application;

public sealed record SaleDetailRequest(string ProductoId, int Cantidad, decimal PrecioUnitario);
public sealed record CreateSaleRequest(IReadOnlyCollection<SaleDetailRequest> Detalles);
public sealed record SaleDetailResponse(string ProductId, int Quantity, decimal UnitPrice, decimal Subtotal);
public sealed record SaleResponse(string Id, DateTime CreatedAtUtc, IReadOnlyCollection<SaleDetailResponse> Details, decimal Total);

public interface ISaleRepository
{
    Task<IReadOnlyCollection<Sale>> ListAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Sale sale, CancellationToken cancellationToken = default);
}

public interface ISaleUseCases
{
    Task<IReadOnlyCollection<SaleResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<SaleResponse> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
}

public sealed class SaleUseCases(ISaleRepository repository) : ISaleUseCases
{
    public async Task<IReadOnlyCollection<SaleResponse>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).Select(ToResponse).ToArray();

    public async Task<SaleResponse> CreateAsync(CreateSaleRequest request, CancellationToken ct = default)
    {
        if (request.Detalles is null)
            throw new ArgumentException("Los detalles de la venta son obligatorios.", nameof(request));

        var sale = Sale.Create(request.Detalles.Select(x =>
            SaleDetail.Create(x.ProductoId, x.Cantidad, x.PrecioUnitario)));
        await repository.CreateAsync(sale, ct);
        return ToResponse(sale);
    }

    private static SaleResponse ToResponse(Sale value) => new(
        value.Id, value.FechaUtc,
        value.Detalles.Select(x => new SaleDetailResponse(
            x.ProductoId, x.Cantidad, x.PrecioUnitario, x.Subtotal)).ToArray(),
        value.Total);
}


