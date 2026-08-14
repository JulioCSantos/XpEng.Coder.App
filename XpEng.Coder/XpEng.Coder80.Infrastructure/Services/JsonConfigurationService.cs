using System;
using System.IO;
using System.Text.Json;

// Both AppConfig and IConfigurationService come from here

namespace XpEng.Coder80.Infrastructure.Services {
    public class JsonConfigurationService : IConfigurationService {
        private readonly string _configFilePath;

        public JsonConfigurationService() {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        }

        public AppConfig Load() {
            if (!File.Exists(_configFilePath)) {
                return new AppConfig();
            }

            try {
                string json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch {
                return new AppConfig();
            }
        }

        public void Save(AppConfig config) {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_configFilePath, json);
        }
    }
}