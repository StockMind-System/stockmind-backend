using StockMind.Forecasting.Application;
using StockMind.Forecasting.Domain.Entities;
using Xunit;

namespace StockMind.UnitTests;

public sealed class ForecastUseCasesTests
{
    [Fact]
    public async Task Create_PreservesAnalyticsTraceability()
    {
        var repository = new InMemoryForecastRepository();
        var useCases = new ForecastUseCases(new StubAnalyticsClient(), repository);
        var request = new CreateForecastRequest(
            "SKU-001", "sku_001", 4, "units",
            [new WeeklyDemandInput(new DateOnly(2026, 9, 28), 10)]);

        var result = await useCases.CreateAsync(request);

        Assert.Equal("SKU-001", result.Sku);
        Assert.Equal(4, result.HorizonWeeks);
        Assert.Equal("xgboost", result.Method);
        Assert.Equal("xgb-dataset-1", result.ModelVersion);
        Assert.Equal("dataset-1", result.DatasetVersion);
        Assert.Equal(4, result.Forecasts.Count);
        Assert.Single(repository.Items);
    }

    private sealed class StubAnalyticsClient : IAnalyticsClient
    {
        public Task<AnalyticsForecast> PredictAsync(AnalyticsForecastRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AnalyticsForecast(
                request.Sku, new DateOnly(2026, 9, 28), 4, "units", "xgboost",
                "xgb-dataset-1", "dataset-1", "ready",
                Enumerable.Range(1, 4)
                    .Select(index => new AnalyticsWeeklyForecast(
                        new DateOnly(2026, 9, 28).AddDays(index * 7), 10 + index))
                    .ToArray()));

        public Task<AnalyticsEvaluation> EvaluateAsync(AnalyticsEvaluationRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class InMemoryForecastRepository : IForecastRepository
    {
        public List<Forecast> Items { get; } = [];
        public Task<IReadOnlyCollection<Forecast>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Forecast>>(Items);
        public Task CreateAsync(Forecast forecast, CancellationToken cancellationToken = default)
        {
            Items.Add(forecast);
            return Task.CompletedTask;
        }
    }
}
