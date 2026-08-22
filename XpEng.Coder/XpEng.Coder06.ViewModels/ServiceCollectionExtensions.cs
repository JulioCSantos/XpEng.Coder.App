using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder09.Models;
using XpEng.Coder12.Services;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder06.ViewModels; 
public static class ServiceCollectionExtensions {
    public static IServiceCollection AddViewModels(this IServiceCollection services) {
        services.AddSingleton(provider => MainViewModel.Instance);
        services = services.AddModels();
        services = services.AddServices();

        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        services.ApplyRegistrations(assembly);
        return services;
    }
}
