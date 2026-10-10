using Microsoft.AspNetCore.Mvc;
using StockMind.Sales.Application;

namespace StockMind.Sales.Api.Controllers;

[ApiController, Route("api/ventas")]
public sealed class SalesController(ISaleUseCases useCases) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyCollection<SaleResponse>> List(CancellationToken ct) => useCases.ListAsync(ct);
    [HttpPost] public async Task<ActionResult<SaleResponse>> Create(CreateSaleRequest request, CancellationToken ct) =>
        Ok(await useCases.CreateAsync(request, ct));
}

