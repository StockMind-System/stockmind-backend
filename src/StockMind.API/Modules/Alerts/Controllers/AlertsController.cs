using Microsoft.AspNetCore.Mvc;
using StockMind.Alerts.Application;

namespace StockMind.Alerts.Api.Controllers;

[ApiController, Route("api/alertas")]
public sealed class AlertsController(IAlertUseCases useCases) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyCollection<ReplenishmentAlertResponse>> List(CancellationToken ct) => useCases.ListAsync(ct);
}

