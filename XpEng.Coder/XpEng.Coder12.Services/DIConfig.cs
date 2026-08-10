using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder12.Services;

public class DIConfig {
    public static IServiceCollection Config(IServiceCollection serviceCollection) {
        serviceCollection.AddTransient<ITemplatesCaller, TemplatesCaller>();

        return serviceCollection;
    }
}