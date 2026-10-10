using Microsoft.AspNetCore.Mvc;
using StockMind.Catalog.Application;

namespace StockMind.Catalog.Api.Controllers;

[ApiController, Route("api/productos")]
public sealed class ProductsController(IProductUseCases useCases) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ProductDto>> List(CancellationToken ct) => useCases.ListAsync(ct);

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> Get(string id, CancellationToken ct) =>
        await useCases.GetAsync(id, ct) is { } item ? Ok(item) : NotFound();

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(SaveProductRequest request, CancellationToken ct)
    {
        var item = await useCases.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct) =>
        await useCases.DeleteAsync(id, ct) ? NoContent() : NotFound();
}

