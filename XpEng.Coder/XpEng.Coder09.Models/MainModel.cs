using CommunityToolkit.Mvvm.DependencyInjection; // Required for Ioc.Default
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder09.Models.Entities;
using XpEng.Coder09.Models.Transport;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder09.Models {
    public class MainModel {

        #region Singleton
        private static readonly Lazy<MainModel> _instance = new(() => new MainModel());
        public static MainModel Instance => _instance.Value;

        private MainModel() {
            PlanOrchestrators = new ObservableCollection<PlanOrchestrator>();
            var configPersistenceTest = ConfigPersistence;
        }
        #endregion Singleton


        public ObservableCollection<PlanOrchestrator> PlanOrchestrators { get; }

        // Lazy instantiation resolving directly from the DI container
        private IConfigPersistence? _configPersistence;
        private IConfigPersistence ConfigPersistence {
            get {
                if (_configPersistence != null) return _configPersistence;

                _configPersistence = DIExtensions.ServiceProvider.GetRequiredService<IConfigPersistence>()
                    ?? throw new InvalidOperationException("IConfigPersistence is not registered in the DI container.");

                //_configPersistence = Ioc.Default.GetService<IConfigPersistence>()
                //                     ?? throw new InvalidOperationException("IConfigPersistence is not registered in the DI container.");

                return _configPersistence;
            }
            // Optional: Setter kept for explicitly injecting mocks during highly isolated unit tests
            set => _configPersistence = value;
        }



        public void SavePlans() {
            var pocos = PlanOrchestrators.Select(plan => plan.ToPoco()).ToList();
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Calls the lazy property, resolving it if it hasn't been already
            ConfigPersistence.SaveConfigJson(JsonSerializer.Serialize(pocos, options));
        }

        public void LoadPlans() {
            string json = ConfigPersistence.LoadConfigJson();
            if (string.IsNullOrWhiteSpace(json)) return;

            var pocos = JsonSerializer.Deserialize<List<PlanOrchestratorPoco>>(json);
            if (pocos == null) return;

            PlanOrchestrators.Clear();
            foreach (var poco in pocos) {
                PlanOrchestrators.Add(new PlanOrchestrator(poco));
            }
        }
    }
}