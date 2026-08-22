using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder80.Infrastructure {
    public interface IDICustomization {
        void Configure(IServiceCollection services);
    }
}
