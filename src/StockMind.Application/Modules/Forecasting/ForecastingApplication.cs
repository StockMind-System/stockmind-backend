using StockMind.Forecasting.Domain.Entities;

namespace StockMind.Forecasting.Application;

public sealed record WeeklyDemandInput(DateOnly WeekStart, double Demand);
public sealed record CreateForecastRequest(
    string Sku, string ModelName, int HorizonWeeks, string Unit,
    IReadOnlyCollection<WeeklyDemandInput> History);
public sealed record AnalyticsWeeklyDemandInput(DateOnly WeekStart, double Demand);
public sealed record AnalyticsForecastRequest(
    string Sku, string ModelName, int HorizonWeeks, string Unit,
    IReadOnlyCollection<AnalyticsWeeklyDemandInput> History);
public sealed record AnalyticsWeeklyForecast(DateOnly WeekStart, double PredictedDemand);
public sealed record AnalyticsForecast(
    string Sku, DateOnly CutoffDate, int HorizonWeeks, string Unit, string Method,
    string ModelVersion, string DatasetVersion, string Status,
    IReadOnlyCollection<AnalyticsWeeklyForecast> Forecasts);
public sealed record EvaluateModelRequest(
    IReadOnlyCollection<double> YTrue,
    IReadOnlyCollection<double> YPred,
    string Model = "demand_model");
public sealed record AnalyticsEvaluationRequest(
    IReadOnlyCollection<double> YTrue,
    IReadOnlyCollection<double> YPred);
public sealed record AnalyticsEvaluation(
    double Mae, double Rmse, double? Wape, double? R2, string? Reason);

public sealed record ForecastResponse(
    string Id, string Sku, DateOnly CutoffDate, int HorizonWeeks, string Unit,
    string Method, string ModelVersion, string DatasetVersion, string Status,
    IReadOnlyCollection<WeeklyForecastResponse> Forecasts, DateTime CreatedAtUtc);
public sealed record WeeklyForecastResponse(DateOnly WeekStart, double PredictedDemand);
public sealed record ModelEvaluationResponse(
    string Id, string Model, double Mae, double Rmse, double? Wape,
    double? R2, string? Reason, DateTime EvaluatedAtUtc);

public interface IAnalyticsClient
{
    Task<AnalyticsForecast> PredictAsync(AnalyticsForecastRequest request, CancellationToken cancellationToken = default);
    Task<AnalyticsEvaluation> EvaluateAsync(AnalyticsEvaluationRequest request, CancellationToken cancellationToken = default);
}
public interface IForecastRepository
{
    Task<IReadOnlyCollection<Forecast>> ListAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Forecast forecast, CancellationToken cancellationToken = default);
}
public interface IEvaluationRepository
{
    Task<IReadOnlyCollection<ModelEvaluation>> ListAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(ModelEvaluation evaluation, CancellationToken cancellationToken = default);
}
public interface IForecastUseCases
{
    Task<IReadOnlyCollection<ForecastResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<ForecastResponse> CreateAsync(CreateForecastRequest request, CancellationToken cancellationToken = default);
}
public interface IEvaluationUseCases
{
    Task<IReadOnlyCollection<ModelEvaluationResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<ModelEvaluationResponse> CreateAsync(EvaluateModelRequest request, CancellationToken cancellationToken = default);
}

public sealed class ForecastUseCases(IAnalyticsClient client, IForecastRepository repository) : IForecastUseCases
{
    public async Task<IReadOnlyCollection<ForecastResponse>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).Select(ToResponse).ToArray();
    public async Task<ForecastResponse> CreateAsync(CreateForecastRequest request, CancellationToken ct = default)
    {
        if (request.History is null || request.History.Count == 0)
            throw new ArgumentException("El historial semanal es obligatorio.", nameof(request));

        var analyticsRequest = new AnalyticsForecastRequest(
            request.Sku, request.ModelName, request.HorizonWeeks, request.Unit,
            request.History.Select(x => new AnalyticsWeeklyDemandInput(x.WeekStart, x.Demand)).ToArray());
        var response = await client.PredictAsync(analyticsRequest, ct);
        var forecast = new Forecast
        {
            Sku = response.Sku,
            CutoffDate = response.CutoffDate,
            HorizonWeeks = response.HorizonWeeks,
            Unit = response.Unit,
            Method = response.Method,
            ModelVersion = response.ModelVersion,
            DatasetVersion = response.DatasetVersion,
            Status = response.Status,
            Forecasts = response.Forecasts.Select(x => new WeeklyForecast
            {
                WeekStart = x.WeekStart,
                PredictedDemand = x.PredictedDemand
            }).ToList()
        };
        await repository.CreateAsync(forecast, ct);
        return ToResponse(forecast);
    }

    private static ForecastResponse ToResponse(Forecast value) => new(
        value.Id, value.Sku, value.CutoffDate, value.HorizonWeeks, value.Unit,
        value.Method, value.ModelVersion, value.DatasetVersion, value.Status,
        value.Forecasts.Select(x => new WeeklyForecastResponse(x.WeekStart, x.PredictedDemand)).ToArray(),
        value.FechaUtc);
}

public sealed class EvaluationUseCases(IAnalyticsClient client, IEvaluationRepository repository) : IEvaluationUseCases
{
    public async Task<IReadOnlyCollection<ModelEvaluationResponse>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).Select(ToResponse).ToArray();
    public async Task<ModelEvaluationResponse> CreateAsync(EvaluateModelRequest request, CancellationToken ct = default)
    {
        if (request.YTrue is null || request.YPred is null ||
            request.YTrue.Count == 0 || request.YTrue.Count != request.YPred.Count)
            throw new ArgumentException("Los valores reales y predichos deben tener la misma longitud no vacia.", nameof(request));

        var response = await client.EvaluateAsync(
            new AnalyticsEvaluationRequest(request.YTrue, request.YPred), ct);
        var evaluation = new ModelEvaluation
        {
            Modelo = request.Model,
            Mae = response.Mae, Rmse = response.Rmse, Wape = response.Wape,
            R2 = response.R2, Reason = response.Reason
        };
        await repository.CreateAsync(evaluation, ct);
        return ToResponse(evaluation);
    }

    private static ModelEvaluationResponse ToResponse(ModelEvaluation value) => new(
        value.Id, value.Modelo, value.Mae, value.Rmse, value.Wape,
        value.R2, value.Reason, value.FechaUtc);
}


