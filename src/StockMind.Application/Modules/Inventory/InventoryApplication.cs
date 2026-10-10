using StockMind.Inventory.Domain.Entities;

namespace StockMind.Inventory.Application;

public sealed record RegisterInventoryMovementRequest(
    string ProductoId, MovementType Tipo, int Cantidad, string? Motivo);
public sealed record InventoryMovementResponse(
    string Id, string ProductId, string Type, int Quantity,
    string? Reason, DateTime CreatedAtUtc);

public interface IInventoryRepository
{
    Task<IReadOnlyCollection<InventoryMovement>> ListAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(InventoryMovement movement, CancellationToken cancellationToken = default);
}

public interface IInventoryUseCases
{
    Task<IReadOnlyCollection<InventoryMovementResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task RegisterAsync(RegisterInventoryMovementRequest request, CancellationToken cancellationToken = default);
}

public sealed class InventoryUseCases(IInventoryRepository repository) : IInventoryUseCases
{
    public async Task<IReadOnlyCollection<InventoryMovementResponse>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).Select(x => new InventoryMovementResponse(
            x.Id, x.ProductoId, x.Tipo.ToString(), x.Cantidad, x.Motivo, x.FechaUtc)).ToArray();

    public Task RegisterAsync(RegisterInventoryMovementRequest request, CancellationToken ct = default) =>
        repository.CreateAsync(
            InventoryMovement.Create(request.ProductoId, request.Tipo, request.Cantidad, request.Motivo),
            ct);
}


