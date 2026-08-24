using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder09.Models.Entities;
using XpEng.Coder09.Models.Transport;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder09.Models;

public class MainModel {

    #region singleton
    public static MainModel Instance => DesignTimeSingleton<MainModel>.Resolve(() => new MainModel());

    internal MainModel() {
        PlanOrchestrators = new ObservableCollection<PlanOrchestrator>();
    }
    #endregion singleton

    public ObservableCollection<PlanOrchestrator> PlanOrchestrators { get; }

    private IConfigPersistence? _configPersistence;
    private IConfigPersistence ConfigPersistence {
        get {
            if (_configPersistence != null) return _configPersistence;

            _configPersistence = DIExtensions.ServiceProvider.GetRequiredService<IConfigPersistence>()
                                 ?? throw new InvalidOperationException("IConfigPersistence is not registered in the DI container.");

            return _configPersistence;
        }
        set => _configPersistence = value;
    }

    public void SavePlans() {
        var pocos = PlanOrchestrators.Select(plan => plan.ToPoco()).ToList();
        var options = new JsonSerializerOptions {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };

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