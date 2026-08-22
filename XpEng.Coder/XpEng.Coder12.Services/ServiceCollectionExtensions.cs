using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder12.Services.T4Pipeline;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder12.Services;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddServices(this IServiceCollection services) {
        services = services.AddInfrastructure();

        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        services.ApplyRegistrations(assembly);
        return services;
    }
}
