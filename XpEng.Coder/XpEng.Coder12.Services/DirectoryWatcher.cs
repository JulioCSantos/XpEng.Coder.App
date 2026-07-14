using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace XpEng.Coder12.Services {
    public class DirectoryWatcher : IDisposable {
        private readonly string _sourceDirectory;
        private readonly string _targetDirectory;
        private readonly ICodeGenerator _generator;
        private readonly Action<string> _logAction;

        private FileSystemWatcher? _watcher;
        // Explicitly declare the namespace to avoid the implicit System.Threading clash
        private System.Timers.Timer? _debounceTimer;
        private readonly ConcurrentDictionary<string, FileChangeEvent> _pendingChanges = new();

        public DirectoryWatcher(string sourceDirectory, string targetDirectory, ICodeGenerator generator, Action<string> logAction) {
            _sourceDirectory = sourceDirectory;
            _targetDirectory = targetDirectory;
            _generator = generator;
            _logAction = logAction;
        }

        public void Start() {
            if (_watcher != null) return;

            // Explicitly instantiate System.Timers.Timer
            _debounceTimer = new System.Timers.Timer(500) { AutoReset = false };
            _debounceTimer.Elapsed += OnTimerElapsed;

            _watcher = new FileSystemWatcher(_sourceDirectory) {
                Filter = "*.cs",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            _watcher.Created += OnFileChanged;
            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _watcher.EnableRaisingEvents = true;
            _logAction($"Started watching: {_sourceDirectory}");
        }

        public void Stop() {
            if (_watcher != null) {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileChanged;
                _watcher.Changed -= OnFileChanged;
                _watcher.Deleted -= OnFileChanged;
                _watcher.Renamed -= OnFileRenamed;
                _watcher.Dispose();
                _watcher = null;
            }

            if (_debounceTimer != null) {
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
                _debounceTimer = null;
            }

            _pendingChanges.Clear();
            _logAction("System offline.");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e) {
            ChangeType changeType = e.ChangeType switch {
                WatcherChangeTypes.Created => ChangeType.Created,
                WatcherChangeTypes.Deleted => ChangeType.Deleted,
                _ => ChangeType.Changed
            };

            AddOrUpdateChange(e.FullPath, changeType);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e) {
            AddOrUpdateChange(e.FullPath, ChangeType.Renamed, e.OldFullPath);
        }

        private void AddOrUpdateChange(string fullPath, ChangeType changeType, string? oldFullPath = null) {
            var changeEvent = new FileChangeEvent {
                FullPath = fullPath,
                OldFullPath = oldFullPath,
                ChangeType = changeType,
                Timestamp = DateTime.Now
            };

            _pendingChanges.AddOrUpdate(fullPath, changeEvent, (_, _) => changeEvent);

            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e) {
            if (_pendingChanges.IsEmpty) return;

            var changesToProcess = new List<FileChangeEvent>();

            foreach (var key in _pendingChanges.Keys.ToList()) {
                if (_pendingChanges.TryRemove(key, out var change)) {
                    changesToProcess.Add(change);
                }
            }

            if (changesToProcess.Any()) {
                _generator.ProcessBatch(changesToProcess, _sourceDirectory, _targetDirectory, _logAction);
            }
        }

        public void Dispose() {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}