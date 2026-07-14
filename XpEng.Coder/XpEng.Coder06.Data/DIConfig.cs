using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using XpEng.Coder09.Models.Interfaces;

namespace XpEng.Coder06.Data;

public class DIConfig {
    // 1. The Data project owns its hardcoded default
    private const string DefaultConnection = "Data Source=asus-strange2;Initial Catalog=XpEng.CoderDb;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";

    // 2. Make the parameter optional
    public static IServiceCollection Config(IServiceCollection serviceCollection, string? overrideConnectionString = null) {
        // 3. Fallback logic: Use the override if provided, otherwise use the internal default
        string activeConnectionString = string.IsNullOrWhiteSpace(overrideConnectionString)
            ? DefaultConnection
            : overrideConnectionString;


        // Fulfills the contractual obligation

        return serviceCollection;
    }
}