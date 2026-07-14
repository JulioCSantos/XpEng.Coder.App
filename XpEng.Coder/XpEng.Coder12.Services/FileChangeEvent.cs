using System;

namespace XpEng.Coder12.Services {
    public class FileChangeEvent {
        public string FullPath { get; set; } = string.Empty;

        public string? OldFullPath { get; set; }

        public ChangeType ChangeType { get; set; }

        public DateTime Timestamp { get; set; }
    }
}