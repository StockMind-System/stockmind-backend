using Microsoft.AspNetCore.Mvc;
using StockMind.Forecasting.Application;

namespace StockMind.Forecasting.Api.Controllers;

[ApiController, Route("api/predicciones")]
public sealed class ForecastsController(IForecastUseCases useCases) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyCollection<ForecastResponse>> List(CancellationToken ct) => useCases.ListAsync(ct);
    [HttpPost]
    public async Task<ActionResult<ForecastResponse>> Create(CreateForecastRequest request, CancellationToken ct) =>
        Ok(await useCases.CreateAsync(request, ct));
}

[ApiController, Route("api/evaluaciones")]
public sealed class EvaluationsController(IEvaluationUseCases useCases) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyCollection<ModelEvaluationResponse>> List(CancellationToken ct) => useCases.ListAsync(ct);
    [HttpPost] public async Task<ActionResult<ModelEvaluationResponse>> Create(EvaluateModelRequest request, CancellationToken ct) =>
        Ok(await useCases.CreateAsync(request, ct));
}

