using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder12.Services.T4Pipeline;

namespace XpEng.Coder12.Services;

public class DIConfig {
    public static IServiceCollection Config(IServiceCollection serviceCollection) {
        serviceCollection.AddTransient<ITemplatesCaller, TemplatesCaller>();
        serviceCollection.AddSingleton<TemplateCompilerService>();
        serviceCollection.AddSingleton<TemplateCallerClient>();

        return serviceCollection;
    }
}