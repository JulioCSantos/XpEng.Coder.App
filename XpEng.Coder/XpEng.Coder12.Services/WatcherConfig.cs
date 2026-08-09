using System;
using System.Collections.Generic;
using System.Text;

namespace XpEng.Coder12.Services {
    public class WatcherConfig(string directoryPath, string planName) {
        public string DirectoryPath { get; } = directoryPath;
        public string PlanName { get; } = planName;
    }
}
