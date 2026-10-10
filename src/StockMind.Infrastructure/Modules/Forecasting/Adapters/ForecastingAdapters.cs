using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StockMind.Forecasting.Application;
using StockMind.Forecasting.Domain.Entities;
using StockMind.Infrastructure.Persistence;

namespace StockMind.Forecasting.Infrastructure.Adapters;

internal sealed class AnalyticsClient(HttpClient httpClient) : IAnalyticsClient
{
    private static readonly JsonSerializerOptions AnalyticsJson = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<AnalyticsForecast> PredictAsync(AnalyticsForecastRequest request, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/prediction/predict", request, AnalyticsJson, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AnalyticsForecast>(AnalyticsJson, ct)
            ?? throw new InvalidOperationException("Analytics devolvio una respuesta vacia.");
    }

    public async Task<AnalyticsEvaluation> EvaluateAsync(AnalyticsEvaluationRequest request, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/evaluation/evaluate", request, AnalyticsJson, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AnalyticsEvaluation>(AnalyticsJson, ct)
            ?? throw new InvalidOperationException("Analytics devolvio una respuesta vacia.");
    }
}

internal sealed class ForecastRepository(IMongoDatabase database) : IForecastRepository
{
    private IMongoCollection<Forecast> Collection => database.GetCollection<Forecast>("predicciones");
    public async Task<IReadOnlyCollection<Forecast>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(Builders<Forecast>.Filter.Empty).ToListAsync(ct);
    public Task CreateAsync(Forecast forecast, CancellationToken ct = default) =>
        Collection.InsertOneAsync(forecast, cancellationToken: ct);
}

internal sealed class EvaluationRepository(IMongoDatabase database) : IEvaluationRepository
{
    private IMongoCollection<ModelEvaluation> Collection => database.GetCollection<ModelEvaluation>("evaluaciones_modelo");
    public async Task<IReadOnlyCollection<ModelEvaluation>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(Builders<ModelEvaluation>.Filter.Empty).ToListAsync(ct);
    public Task CreateAsync(ModelEvaluation evaluation, CancellationToken ct = default) =>
        Collection.InsertOneAsync(evaluation, cancellationToken: ct);
}

public static class DependencyInjection
{
    public static IServiceCollection AddForecastingInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Analytics:BaseUrl"] ?? "http://localhost:8000";
        services.AddHttpClient<IAnalyticsClient, AnalyticsClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IForecastRepository, ForecastRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        return services;
    }
}

