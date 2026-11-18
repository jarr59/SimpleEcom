using Microsoft.EntityFrameworkCore;
using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Shared.Contracts;

namespace SimpleEcom.Api.Infrastructure.Persistence;

/// <summary>Aplica las migraciones pendientes y carga datos de ejemplo si la tabla de productos está vacía.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SimpleEcomDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = SampleProducts
            .Select(s => Product.Create(s.Sku, s.Name, s.Description, Money.Create(s.Price, Currency.USD).Value, s.Stock, timeProvider))
            .Select(r => r.Value)
            .ToList();

        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cargados {Count} productos de ejemplo.", products.Count);
    }

    private static readonly (string Sku, string Name, string Description, decimal Price, int Stock)[] SampleProducts =
    [
        ("TSHIRT-001", "Camiseta básica", "Camiseta de algodón 100 %, corte clásico.", 19.99m, 120),
        ("TSHIRT-002", "Camiseta premium", "Algodón orgánico peinado, tacto suave.", 29.99m, 60),
        ("HOODIE-001", "Sudadera con capucha", "Felpa de 350 g con bolsillo canguro.", 49.90m, 35),
        ("CAP-001", "Gorra ajustable", "Gorra de seis paneles con cierre metálico.", 15.00m, 80),
        ("MUG-001", "Taza cerámica", "Taza de 350 ml apta para lavavajillas.", 9.50m, 200),
        ("BOTTLE-001", "Botella térmica", "Acero inoxidable, mantiene el frío 24 h.", 24.95m, 45),
        ("BAG-001", "Bolsa de tela", "Tote bag de lona resistente.", 12.00m, 150),
        ("SOCKS-001", "Pack de calcetines", "Tres pares de algodón, talla única.", 11.99m, 0)
    ];
}
