using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder09.Models {
    public class DIConfig {

        public static IServiceCollection Config(IServiceCollection serviceCollection) {
            serviceCollection = Coder80.Infrastructure.DIConfig.Config(serviceCollection);
            serviceCollection.AddSingleton<IConfigPersistence, JsonConfigPersistence>();
            serviceCollection.AddSingleton(provider => MainModel.Instance);
            return serviceCollection;
        }
    }
}