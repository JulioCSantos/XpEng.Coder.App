using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace XpEng.Coder80.Infrastructure;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services) {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        services.ApplyRegistrations(assembly);
        return services;
    }

    public static IServiceCollection ApplyRegistrations(this IServiceCollection services, Assembly assembly) {
        var types = assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false });
        foreach (var type in types) {
            foreach (var attr in type.GetCustomAttributes<RegisterAttribute>()) {
                var serviceType = attr.ServiceType ?? type;
                switch (attr.Lifetime) {
                    case ServiceLifetime.Singleton: services.AddSingleton(serviceType, type); break;
                    case ServiceLifetime.Scoped: services.AddScoped(serviceType, type); break;
                    case ServiceLifetime.Transient: services.AddTransient(serviceType, type); break;
                }
            }
        }
        return services;
    }
}