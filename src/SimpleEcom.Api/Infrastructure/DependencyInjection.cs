using Microsoft.EntityFrameworkCore;
using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Api.Infrastructure.Persistence;

namespace SimpleEcom.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SimpleEcom")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:SimpleEcom'.");

        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<SimpleEcomDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IProductRepository, SqlProductRepository>();
        return services;
    }
}
