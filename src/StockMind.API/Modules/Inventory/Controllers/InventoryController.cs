using Microsoft.AspNetCore.Mvc;
using StockMind.Inventory.Application;

namespace StockMind.Inventory.Api.Controllers;

[ApiController, Route("api/inventario")]
public sealed class InventoryController(IInventoryUseCases useCases) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<InventoryMovementResponse>> List(CancellationToken ct) => useCases.ListAsync(ct);

    [HttpPost]
    public async Task<IActionResult> Register(RegisterInventoryMovementRequest request, CancellationToken ct)
    {
        await useCases.RegisterAsync(request, ct);
        return Accepted();
    }
}

