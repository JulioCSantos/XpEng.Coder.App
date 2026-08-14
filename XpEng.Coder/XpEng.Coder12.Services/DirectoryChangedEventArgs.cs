using System;

namespace XpEng.Coder12.Services {
    public readonly record struct DirectoryChangedEventArgs
        (Guid WatcherId, string FileName, string ChangeType, string? OldFileName = null);
}