using StockMind.Catalog.Domain.Entities;
using StockMind.Inventory.Domain.Entities;
using StockMind.Sales.Domain.Entities;
using Xunit;

namespace StockMind.UnitTests;

public sealed class DomainRulesTests
{
    [Fact]
    public void Product_NormalizesSkuAndRejectsNegativeStock()
    {
        var product = Product.Create(" sku-01 ", "Producto", null, 10m, 2, 1);

        Assert.Equal("SKU-01", product.Sku);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create("SKU-02", "Producto", null, 10m, -1, 1));
    }

    [Fact]
    public void InventoryMovement_RequiresPositiveQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InventoryMovement.Create("product-1", MovementType.Entrada, 0, null));
    }

    [Fact]
    public void Sale_RequiresDetailsAndCalculatesTotal()
    {
        Assert.Throws<ArgumentException>(() => Sale.Create([]));

        var sale = Sale.Create([SaleDetail.Create("product-1", 2, 12.5m)]);

        Assert.Equal(25m, sale.Total);
    }
}
