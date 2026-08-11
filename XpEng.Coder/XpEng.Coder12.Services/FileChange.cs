namespace XpEng.Coder12.Services {
    public class FileChange(string sourceFilePath, string changeType, string? oldSourceFilePath = null) {
        public string SourceFilePath { get; } = sourceFilePath;
        public string ChangeType { get; } = changeType;
        public string? OldSourceFilePath { get; } = oldSourceFilePath;
    }
}