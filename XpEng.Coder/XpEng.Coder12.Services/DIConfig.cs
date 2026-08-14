using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder12.Services;

public class DIConfig {
    public static IServiceCollection Config(IServiceCollection serviceCollection) {
        serviceCollection.AddTransient<ITemplatesCaller, TemplatesCaller>();
        serviceCollection.AddSingleton<TemplateCompilerService>();
        serviceCollection.AddSingleton<TemplateCallerClient>();

        return serviceCollection;
    }
}