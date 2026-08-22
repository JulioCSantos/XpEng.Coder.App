using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder12.Services.T4Pipeline;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder12.Services; 
public static class ServiceCollectionExtensions {
    public static IServiceCollection AddServices(this IServiceCollection services) {
        services.AddTransient<ITemplatesCaller, TemplatesCaller>();
        services.AddSingleton<TemplateCompilerService>();
        services.AddSingleton<TemplateCallerClient>();
        services = services.AddInfrastructure();
    

        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        services.ApplyDICustomizations(assembly);
        return services;
    }
}
