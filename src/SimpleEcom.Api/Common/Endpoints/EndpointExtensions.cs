using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleEcom.Api.Common.Endpoints;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var descriptors = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(t => ServiceDescriptor.Transient(typeof(IEndpoint), t));

        services.TryAddEnumerable(descriptors);
        return services;
    }

    /// <summary>Registra los handlers de los slices (clases que terminan en "Handler" dentro de Features).</summary>
    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlers = assembly.DefinedTypes.Where(t =>
            t is { IsAbstract: false, IsInterface: false, IsClass: true } &&
            t.Namespace?.Contains(".Features.", StringComparison.Ordinal) == true &&
            t.Name.EndsWith("Handler", StringComparison.Ordinal));

        foreach (var handler in handlers)
        {
            services.TryAddScoped(handler);
        }

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app, RouteGroupBuilder? group = null)
    {
        IEndpointRouteBuilder builder = group is null ? app : group;

        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }
}
