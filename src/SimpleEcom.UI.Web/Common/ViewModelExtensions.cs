using System.Reflection;

namespace SimpleEcom.UI.Web.Common;

public static class ViewModelExtensions
{
    /// <summary>Registra como Transient todos los ViewModels (subclases de <see cref="ViewModelBase"/>) del ensamblado.</summary>
    public static IServiceCollection AddViewModels(this IServiceCollection services, Assembly assembly)
    {
        var viewModels = assembly.DefinedTypes.Where(t =>
            t is { IsAbstract: false, IsClass: true } && t.IsAssignableTo(typeof(ViewModelBase)));

        foreach (var viewModel in viewModels)
        {
            services.AddTransient(viewModel);
        }

        return services;
    }
}
