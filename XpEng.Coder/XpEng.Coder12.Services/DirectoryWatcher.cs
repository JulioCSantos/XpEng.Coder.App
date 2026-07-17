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
        private readonly string _templatePath;
        private readonly ITemplatesCaller _generator;
        private readonly Action<string> _logAction;

        private FileSystemWatcher? _watcher;
        // Explicitly declare the namespace to avoid the implicit System.Threading clash
        private System.Timers.Timer? _debounceTimer;
        private readonly ConcurrentDictionary<string, FileChangeEvent> _pendingChanges = new();


        public DirectoryWatcher(string sourceDirectory, string targetDirectory, string templatePath, ITemplatesCaller generator, Action<string> logAction) {
            _sourceDirectory = sourceDirectory;
            _targetDirectory = targetDirectory;
            _templatePath = templatePath; // Store it here
            _generator = generator;
            _logAction = logAction;
        }

        public void Start() {
            if (_watcher != null) return;

            // Explicitly instantiate System.Timers.Timer
            _debounceTimer = new System.Timers.Timer(500) { AutoReset = false };
            _debounceTimer.Elapsed += OnTimerElapsed;

            _watcher = new FileSystemWatcher(_sourceDirectory) {
                Filter = "*.*", // Changed from "*.cs" to watch everything
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
            if (IsTempFile(e.FullPath)) return;

            ChangeType changeType = e.ChangeType switch {
                WatcherChangeTypes.Created => ChangeType.Created,
                WatcherChangeTypes.Deleted => ChangeType.Deleted,
                _ => ChangeType.Changed
            };

            AddOrUpdateChange(e.FullPath, changeType);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e) {
            if (IsTempFile(e.FullPath)) return;

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

        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e) {
            if (_pendingChanges.IsEmpty) return;

            var changesToProcess = new List<FileChangeEvent>();

            foreach (var key in _pendingChanges.Keys.ToList()) {
                if (_pendingChanges.TryRemove(key, out var change)) {
                    changesToProcess.Add(change);
                }
            }

            if (changesToProcess.Any()) {
                foreach (var change in changesToProcess) {

                    // Handle the special Rename rule: split into Deleted and Created
                    if (change.ChangeType == ChangeType.Renamed && !string.IsNullOrEmpty(change.OldFullPath)) {
                        _logAction($"Translating rename to Delete/Create for: {Path.GetFileName(change.FullPath)}");

                        // 1. Delete the old file
                        await _generator.ProcessFileAsync(change.OldFullPath, _templatePath, _targetDirectory, "Deleted", _logAction);

                        // 2. Create the new file
                        await _generator.ProcessFileAsync(change.FullPath, _templatePath, _targetDirectory, "Created", _logAction);
                    }
                    else {
                        // Standard Created, Deleted, or Changed processing
                        // Notice the 4th argument is now the string representation of the change type
                        await _generator.ProcessFileAsync(change.FullPath, _templatePath, _targetDirectory, change.ChangeType.ToString(), _logAction);
                    }
                }
            }
        }

        private bool IsTempFile(string filePath) {
            if (string.IsNullOrEmpty(filePath)) return false;

            string fileName = System.IO.Path.GetFileName(filePath);

            // Reject files containing ~ or ending in .tmp
            return fileName.Contains("~") ||
                   filePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose() {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}