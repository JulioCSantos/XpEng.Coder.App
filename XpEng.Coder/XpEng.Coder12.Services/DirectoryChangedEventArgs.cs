using System;

namespace XpEng.Coder12.Services {

    // Lives in Tier 12. Knows NOTHING about Tier 09.
    public class DirectoryChangedEventArgs(Guid watcherId, string fileName, string changeType) : EventArgs {
        public Guid WatcherId { get; } = watcherId;
        public string FileName { get; } = fileName;
        public string ChangeType { get; } = changeType;
    }
}