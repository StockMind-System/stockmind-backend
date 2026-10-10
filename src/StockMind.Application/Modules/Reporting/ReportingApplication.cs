using StockMind.Reporting.Domain;

namespace StockMind.Reporting.Application;

public interface IReportReader
{
    Task<ReportSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}
public interface IReportUseCases
{
    Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
public sealed record ReportSummaryResponse(long Products, long Sales, decimal Revenue);
public sealed class ReportUseCases(IReportReader reader) : IReportUseCases
{
    public async Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken ct = default)
    {
        var summary = await reader.GetSummaryAsync(ct);
        return new ReportSummaryResponse(summary.Productos, summary.Ventas, summary.Ingresos);
    }
}

