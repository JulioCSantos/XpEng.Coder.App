using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using XpEng.Coder09.Models.Interfaces;

namespace XpEng.Coder06.Data;

public class DIConfig {
    private const string DefaultConnection = "Data Source=asus-strange2;Initial Catalog=XpEng.CoderDb;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";

    public static IServiceCollection Config(IServiceCollection serviceCollection, string? overrideConnectionString = null) {
       
        serviceCollection = Coder09.Models.DIConfig.Config(serviceCollection);

        string activeConnectionString = string.IsNullOrWhiteSpace(overrideConnectionString)
            ? DefaultConnection
            : overrideConnectionString;

        return serviceCollection;
    }
}