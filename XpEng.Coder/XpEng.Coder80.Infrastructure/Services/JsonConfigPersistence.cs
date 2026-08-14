using System;
using System.Collections.Generic;
using System.Text;
using XpEng.Coder80.Infrastructure.Interfaces;

namespace XpEng.Coder80.Infrastructure.Services {
    public class JsonConfigPersistence : IConfigPersistence {

        private FileInfo? _configFile;

        private FileInfo ConfigFile {
            get {
                if (_configFile != null) return _configFile;

                string appData = AppDomain.CurrentDomain.BaseDirectory;
                _configFile = new FileInfo(Path.Combine(appData, "CoderPlansConfig.json"));
                return _configFile;
            }
        }

        public string LoadConfigJson() {
            if (!ConfigFile.Exists) return string.Empty;
            return File.ReadAllText(ConfigFile.FullName);
        }

        public void SaveConfigJson(string json) {
            File.WriteAllText(ConfigFile.FullName, json);
        }
    }
}
