using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using XpEng.Coder80.Infrastructure;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder09.Models {
    public static class ServiceCollectionExtensions {
        public static IServiceCollection AddModels(this IServiceCollection services) {
            services.AddSingleton<IConfigPersistence, JsonConfigPersistence>();
            services.AddSingleton(provider => MainModel.Instance);
            services = services.AddInfrastructure();

            var assembly = typeof(ServiceCollectionExtensions).Assembly;
            services.ApplyRegistrations(assembly);
            return services;
        }
    }
}
