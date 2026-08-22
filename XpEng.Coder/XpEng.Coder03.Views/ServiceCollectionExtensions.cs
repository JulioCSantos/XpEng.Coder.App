using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using XpEng.Coder06.Data;
using XpEng.Coder06.ViewModels;
using XpEng.Coder12.Services.T4Pipeline;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder03.Views;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddViews(this IServiceCollection services) {
        services.AddSingleton(typeof(MainView));
        services = services.AddViewModels();
        services = services.AddData();

        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        services.ApplyDICustomizations(assembly);
        return services;
    }
}
