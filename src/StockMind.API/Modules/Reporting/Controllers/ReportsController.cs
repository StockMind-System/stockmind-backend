using Microsoft.AspNetCore.Mvc;
using StockMind.Reporting.Application;

namespace StockMind.Reporting.Api.Controllers;

[ApiController, Route("api/reportes")]
public sealed class ReportsController(IReportUseCases useCases) : ControllerBase
{
    [HttpGet("resumen")]
    public Task<ReportSummaryResponse> Summary(CancellationToken ct) => useCases.GetSummaryAsync(ct);
}

