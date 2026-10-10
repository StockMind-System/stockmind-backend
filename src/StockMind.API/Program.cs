using System.Text.Json.Serialization;
using StockMind.Alerts.Application;
using StockMind.Alerts.Infrastructure.Persistence;
using StockMind.Api.Middleware;
using StockMind.Api.Configuration;
using StockMind.Catalog.Application;
using StockMind.Catalog.Infrastructure.Persistence;
using StockMind.Forecasting.Application;
using StockMind.Forecasting.Infrastructure.Adapters;
using StockMind.Identity.Application;
using StockMind.Identity.Infrastructure.Security;
using StockMind.Infrastructure.Persistence;
using StockMind.Inventory.Application;
using StockMind.Inventory.Infrastructure.Persistence;
using StockMind.Reporting.Application;
using StockMind.Reporting.Infrastructure.Persistence;
using StockMind.Sales.Application;
using StockMind.Sales.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "Frontend";
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy(frontendCorsPolicy, policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

builder.Services
    .AddStockMindInfrastructure(builder.Configuration)
    .AddApplicationUseCases()
    .AddIdentityInfrastructure(builder.Configuration)
    .AddCatalogInfrastructure()
    .AddInventoryInfrastructure()
    .AddSalesInfrastructure()
    .AddForecastingInfrastructure(builder.Configuration)
    .AddAlertsInfrastructure()
    .AddReportingInfrastructure();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();

public partial class Program;
