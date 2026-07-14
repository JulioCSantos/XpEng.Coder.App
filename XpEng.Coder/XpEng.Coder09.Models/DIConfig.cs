using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder09.Models {
    public class DIConfig {

        public static IServiceCollection Config(IServiceCollection serviceCollection) {
            serviceCollection = serviceCollection.AddSingleton(typeof(MainModel));
            return serviceCollection;
        }
    }
}
