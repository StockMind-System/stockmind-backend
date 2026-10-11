using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace StockMind.Infrastructure.Persistence;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "stockmind";
    public bool InitializeOnStartup { get; set; } = true;
}

public static class DependencyInjection
{
    public static IServiceCollection AddStockMindInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(MongoDbSettings.SectionName);
        var settings = section.Get<MongoDbSettings>() ?? new MongoDbSettings();
        services.Configure<MongoDbSettings>(section);
        services.Configure<SeedDataSettings>(configuration.GetSection(SeedDataSettings.SectionName));
        services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName));
        services.AddScoped<IMongoDbInitializer, MongoDbInitializer>();
        return services;
    }
}


