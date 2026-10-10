using StockMind.Alerts.Application;
using StockMind.Catalog.Application;
using StockMind.Forecasting.Application;
using StockMind.Identity.Application;
using StockMind.Inventory.Application;
using StockMind.Reporting.Application;
using StockMind.Sales.Application;

namespace StockMind.Api.Configuration;

public static class ApplicationServices
{
    public static IServiceCollection AddApplicationUseCases(this IServiceCollection services)
    {
        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<IProductUseCases, ProductUseCases>();
        services.AddScoped<IInventoryUseCases, InventoryUseCases>();
        services.AddScoped<ISaleUseCases, SaleUseCases>();
        services.AddScoped<IForecastUseCases, ForecastUseCases>();
        services.AddScoped<IEvaluationUseCases, EvaluationUseCases>();
        services.AddScoped<IAlertUseCases, AlertUseCases>();
        services.AddScoped<IReportUseCases, ReportUseCases>();
        return services;
    }
}
