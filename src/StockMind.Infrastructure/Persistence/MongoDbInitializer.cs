using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using StockMind.Catalog.Domain.Entities;
using StockMind.Forecasting.Domain.Entities;
using StockMind.Identity.Application;
using StockMind.Identity.Domain.Entities;
using StockMind.Inventory.Domain.Entities;
using StockMind.Sales.Domain.Entities;

namespace StockMind.Infrastructure.Persistence;

public sealed class SeedDataSettings
{
    public const string SectionName = "SeedData";
    public bool Enabled { get; set; }
    public int RandomSeed { get; set; } = 202610;
    public string Version { get; set; } = "2026.10";
    public string DefaultPassword { get; set; } = "StockMind-123";
}

public interface IMongoDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

internal sealed class MongoDbInitializer(
    IMongoDatabase database,
    IPasswordHasher passwordHasher,
    IOptions<SeedDataSettings> seedOptions,
    ILogger<MongoDbInitializer> logger) : IMongoDbInitializer
{
    private readonly SeedDataSettings _seed = seedOptions.Value;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await CreateIndexesAsync(ct);
        if (!_seed.Enabled)
        {
            logger.LogInformation("La carga de datos sinteticos esta deshabilitada.");
            return;
        }

        var categories = CreateCategories();
        var products = CreateProducts();
        var sales = CreateSales(products);
        await SeedWhenEmptyAsync("categorias", categories, ct);
        await SeedWhenEmptyAsync("productos", products, ct);
        await SeedWhenEmptyAsync("usuarios", CreateUsers(), ct);
        await SeedWhenEmptyAsync("movimientos_inventario", CreateMovements(products, sales), ct);
        await SeedWhenEmptyAsync("ventas", sales, ct);
        await SeedWhenEmptyAsync("predicciones", CreateForecasts(products), ct);

        var metadata = database.GetCollection<BsonDocument>("metadata_sistema");
        var document = new BsonDocument
        {
            ["_id"] = "synthetic-seed",
            ["version"] = _seed.Version,
            ["randomSeed"] = _seed.RandomSeed,
            ["isSynthetic"] = true,
            ["businessTimeZone"] = "America/Lima",
            ["generatedAtUtc"] = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)
        };
        await metadata.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "synthetic-seed"),
            document,
            new ReplaceOptions { IsUpsert = true },
            ct);

        logger.LogInformation(
            "Datos sinteticos StockMind disponibles. Version {Version}, semilla {RandomSeed}.",
            _seed.Version,
            _seed.RandomSeed);
    }

    private async Task CreateIndexesAsync(CancellationToken ct)
    {
        await database.GetCollection<Product>("productos").Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(x => x.Sku),
                new CreateIndexOptions { Name = "ux_productos_sku", Unique = true }),
            new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(x => x.CategoriaId).Ascending(x => x.Activo),
                new CreateIndexOptions { Name = "ix_productos_categoria_activo" })
        ], ct);

        await database.GetCollection<Category>("categorias").Indexes.CreateOneAsync(
            new CreateIndexModel<Category>(
                Builders<Category>.IndexKeys.Ascending(x => x.Nombre),
                new CreateIndexOptions { Name = "ux_categorias_nombre", Unique = true }),
            cancellationToken: ct);

        await database.GetCollection<User>("usuarios").Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.Email),
                new CreateIndexOptions { Name = "ux_usuarios_email", Unique = true }),
            cancellationToken: ct);

        await database.GetCollection<Sale>("ventas").Indexes.CreateOneAsync(
            new CreateIndexModel<Sale>(
                Builders<Sale>.IndexKeys.Descending(x => x.FechaUtc),
                new CreateIndexOptions { Name = "ix_ventas_fecha" }),
            cancellationToken: ct);

        await database.GetCollection<InventoryMovement>("movimientos_inventario").Indexes.CreateOneAsync(
            new CreateIndexModel<InventoryMovement>(
                Builders<InventoryMovement>.IndexKeys
                    .Ascending(x => x.ProductoId)
                    .Descending(x => x.FechaUtc),
                new CreateIndexOptions { Name = "ix_movimientos_producto_fecha" }),
            cancellationToken: ct);

        await database.GetCollection<Forecast>("predicciones").Indexes.CreateOneAsync(
            new CreateIndexModel<Forecast>(
                Builders<Forecast>.IndexKeys
                    .Ascending(x => x.Sku)
                    .Descending(x => x.CutoffDate),
                new CreateIndexOptions { Name = "ix_predicciones_sku_corte" }),
            cancellationToken: ct);
    }

    private async Task SeedWhenEmptyAsync<T>(
        string collectionName,
        IReadOnlyCollection<T> items,
        CancellationToken ct)
    {
        var collection = database.GetCollection<T>(collectionName);
        if (await collection.CountDocumentsAsync(FilterDefinition<T>.Empty, cancellationToken: ct) > 0)
            return;

        await collection.InsertManyAsync(items, cancellationToken: ct);
        logger.LogInformation(
            "Coleccion {CollectionName} inicializada con {ItemCount} registros sinteticos.",
            collectionName,
            items.Count);
    }

    private static IReadOnlyCollection<Category> CreateCategories() =>
    [
        new() { Id = "cat-herramientas", Nombre = "Herramientas", Descripcion = "Herramientas manuales y electricas" },
        new() { Id = "cat-fijaciones", Nombre = "Fijaciones", Descripcion = "Tornillos, clavos y accesorios" },
        new() { Id = "cat-plomeria", Nombre = "Plomeria", Descripcion = "Conexiones, tuberias y griferia" },
        new() { Id = "cat-electricidad", Nombre = "Electricidad", Descripcion = "Cables y accesorios electricos" },
        new() { Id = "cat-seguridad", Nombre = "Seguridad", Descripcion = "Equipos de proteccion personal" }
    ];

    private static IReadOnlyCollection<Product> CreateProducts() =>
    [
        CreateProduct("prod-broca-6", "BR-HSS-006", "Broca HSS 6 mm", "cat-herramientas", 12.90m, 24, 40),
        CreateProduct("prod-martillo-16", "HE-MAR-016", "Martillo de una 16 oz", "cat-herramientas", 38.50m, 36, 12),
        CreateProduct("prod-alicate-8", "HE-ALI-008", "Alicate universal 8 pulg.", "cat-herramientas", 29.90m, 28, 10),
        CreateProduct("prod-tornillo-1", "FI-TOR-001", "Tornillo drywall 1 pulg. · ciento", "cat-fijaciones", 16.50m, 68, 25),
        CreateProduct("prod-clavo-2", "FI-CLA-002", "Clavo para madera 2 pulg. · kg", "cat-fijaciones", 8.90m, 54, 20),
        CreateProduct("prod-codo-pvc", "PL-COD-050", "Codo PVC 1/2 pulg.", "cat-plomeria", 2.80m, 18, 30),
        CreateProduct("prod-tubo-pvc", "PL-TUB-100", "Tubo PVC 1 pulg. · 3 m", "cat-plomeria", 18.90m, 31, 15),
        CreateProduct("prod-grifo", "PL-GRI-001", "Grifo lavadero cromado", "cat-plomeria", 45.00m, 16, 8),
        CreateProduct("prod-cable-12", "EL-CAB-012", "Cable THW N.° 12 · metro", "cat-electricidad", 3.50m, 140, 60),
        CreateProduct("prod-tomacorriente", "EL-TOM-001", "Tomacorriente doble", "cat-electricidad", 14.90m, 42, 18),
        CreateProduct("prod-guante", "SE-GUA-001", "Guante de trabajo · par", "cat-seguridad", 11.50m, 0, 20),
        CreateProduct("prod-lentes", "SE-LEN-001", "Lentes de seguridad", "cat-seguridad", 9.90m, 33, 12)
    ];

    private static Product CreateProduct(
        string id,
        string sku,
        string name,
        string categoryId,
        decimal price,
        int currentStock,
        int minimumStock)
    {
        var product = Product.Create(sku, name, categoryId, price, currentStock, minimumStock);
        product.Id = id;
        return product;
    }

    private IReadOnlyCollection<User> CreateUsers() =>
    [
        CreateUser("user-admin", "Dayana Armas", "dayana@stockmind.demo", "ADMIN"),
        CreateUser("user-sales", "Milton Aquino", "milton@stockmind.demo", "USER"),
        CreateUser("user-inventory", "Oscar Alvarado", "oscar@stockmind.demo", "USER"),
        CreateUser("user-analyst", "Isamar Jacobo", "isamar@stockmind.demo", "ANALYST")
    ];

    private User CreateUser(string id, string name, string email, string role) => new()
    {
        Id = id,
        Nombre = name,
        Email = email,
        Rol = role,
        Activo = true,
        PasswordHash = passwordHasher.Hash(_seed.DefaultPassword)
    };

    private static IReadOnlyCollection<InventoryMovement> CreateMovements(
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<Sale> sales)
    {
        var anchor = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
        var soldByProduct = sales
            .SelectMany(sale => sale.Detalles)
            .GroupBy(detail => detail.ProductoId)
            .ToDictionary(group => group.Key, group => group.Sum(detail => detail.Cantidad));

        var movements = products.Select((product, index) => new InventoryMovement
        {
            Id = $"seed-movement-{index + 1:D3}",
            ProductoId = product.Id,
            Tipo = MovementType.Entrada,
            Cantidad = product.StockActual + soldByProduct.GetValueOrDefault(product.Id),
            Motivo = "Saldo inicial sintetico",
            FechaUtc = anchor.AddDays(-120).AddMinutes(index)
        }).ToList();

        movements.AddRange(sales
            .SelectMany(sale => sale.Detalles.Select((detail, detailIndex) => new InventoryMovement
            {
                Id = $"seed-movement-{sale.Id}-{detailIndex + 1:D2}",
                ProductoId = detail.ProductoId,
                Tipo = MovementType.Salida,
                Cantidad = detail.Cantidad,
                Motivo = $"Venta sintetica {sale.Id}",
                FechaUtc = sale.FechaUtc
            })));

        return movements;
    }

    private IReadOnlyCollection<Sale> CreateSales(IReadOnlyCollection<Product> products)
    {
        var random = new Random(_seed.RandomSeed);
        var anchor = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);
        var sales = new List<Sale>();

        for (var day = 0; day < 120; day++)
        {
            var salesForDay = 1 + random.Next(3);
            for (var number = 0; number < salesForDay; number++)
            {
                var selected = products.ElementAt(random.Next(products.Count));
                var quantity = 1 + random.Next(4);
                var sale = Sale.Create([SaleDetail.Create(selected.Id, quantity, selected.Precio)]);
                sale.Id = $"seed-sale-{day:D3}-{number:D2}";
                sale.FechaUtc = anchor.AddDays(-day).AddMinutes(-(number * 17 + random.Next(60)));
                sales.Add(sale);
            }
        }

        return sales;
    }

    private static IReadOnlyCollection<Forecast> CreateForecasts(
        IReadOnlyCollection<Product> products)
    {
        var cutoff = new DateOnly(2026, 10, 5);
        return products.Take(8).Select((product, index) => new Forecast
        {
            Id = $"seed-forecast-{index + 1:D2}",
            Sku = product.Sku,
            CutoffDate = cutoff,
            HorizonWeeks = 4,
            Unit = "units",
            Method = "moving_average_4",
            ModelVersion = "baseline-synthetic-1",
            DatasetVersion = "synthetic-2026.10",
            Status = "synthetic_demo",
            FechaUtc = new DateTime(2026, 10, 10, 12, index, 0, DateTimeKind.Utc),
            Forecasts = Enumerable.Range(1, 4).Select(week => new WeeklyForecast
            {
                WeekStart = cutoff.AddDays(week * 7),
                PredictedDemand = 8 + index + week
            }).ToList()
        }).ToArray();
    }
}
