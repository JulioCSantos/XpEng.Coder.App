using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace XpEng.Coder80.Infrastructure;
public static class ServiceCollectionExtensions {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services) {
        services.AddSingleton<Interfaces.IConfigPersistence, Services.JsonConfigPersistence>();
        services.AddSingleton<Interfaces.IEngineLogger, Services.EngineLogger>();

        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        services.ApplyDICustomizations(assembly);
        return services;
    }

    public static IServiceCollection ApplyDICustomizations(this IServiceCollection services, Assembly assembly) {
        var types = assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IDICustomization).IsAssignableFrom(t));
        foreach (var type in types) ((IDICustomization)Activator.CreateInstance(type)!).Configure(services);
        return services;
    }
}
