using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using XpEng.Coder09.Models;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder06.Data;

public static class ServiceCollectionExtensions {
    private const string DefaultConnection = "Data Source=asus-strange2;Initial Catalog=XpEng.CoderDb;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";

    public static IServiceCollection AddData(this IServiceCollection services, string? overrideConnectionString = null) {
        services = services.AddModels();
        string activeConnectionString = string.IsNullOrWhiteSpace(overrideConnectionString)
            ? DefaultConnection
            : overrideConnectionString; 

        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        services.ApplyRegistrations(assembly);
        return services;
    }
}
