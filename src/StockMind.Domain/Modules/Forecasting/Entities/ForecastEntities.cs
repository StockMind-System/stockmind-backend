namespace StockMind.Forecasting.Domain.Entities;

public sealed class Forecast
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Sku { get; set; } = string.Empty;
    public DateOnly CutoffDate { get; set; }
    public int HorizonWeeks { get; set; }
    public string Unit { get; set; } = "units";
    public string Method { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public string DatasetVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<WeeklyForecast> Forecasts { get; set; } = [];
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WeeklyForecast
{
    public DateOnly WeekStart { get; set; }
    public double PredictedDemand { get; set; }
}

public sealed class ModelEvaluation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Modelo { get; set; } = string.Empty;
    public double Mae { get; set; }
    public double Rmse { get; set; }
    public double? Wape { get; set; }
    public double? R2 { get; set; }
    public string? Reason { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}

