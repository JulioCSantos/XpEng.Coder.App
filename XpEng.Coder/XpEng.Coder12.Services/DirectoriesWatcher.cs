using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;
// Add your other required using statements for WatcherConfig and DirectoryChangedEventArgs

namespace XpEng.Coder12.Services {
    public class DirectoriesWatcher : IDisposable {
        private readonly Dictionary<Guid, FileSystemWatcher> _watchers = new();
        private readonly Dictionary<string, DateTime> _lastEventTimes = new();
        private readonly TimeSpan _debounceThreshold = TimeSpan.FromMilliseconds(50);
        private readonly object _lock = new();

        // Channel for asynchronous event streaming to the ViewModel
        private readonly Channel<DirectoryChangedEventArgs> _eventChannel = Channel.CreateUnbounded<DirectoryChangedEventArgs>();

        // Dynamically resolve logger exactly like the ViewModel does
        private IEngineLogger Logger => DIExtensions.ServiceProvider.GetRequiredService<IEngineLogger>();

        public bool IsRunning { get; private set; }

        /// <summary>
        /// Starts monitoring and syncs the given configuration.
        /// Expected by DashboardViewModel.ToggleWatch()
        /// </summary>
        public void Start(Dictionary<Guid, WatcherConfig> directoriesToWatch) {
            IsRunning = true;
            SyncWatchers(directoriesToWatch);
        }

        /// <summary>
        /// Stops all monitoring and destroys OS handles.
        /// Expected by DashboardViewModel.ToggleWatch()
        /// </summary>
        public void Stop() {
            IsRunning = false;
            lock (_lock) {
                foreach (var id in _watchers.Keys.ToList()) {
                    StopAndDisposeWatcher(id);
                }
                _watchers.Clear();
                _lastEventTimes.Clear();
            }
        }

        /// <summary>
        /// Exposes the channel reader as an IAsyncEnumerable.
        /// Expected by DashboardViewModel.ConsumeWatcherEventsAsync()
        /// </summary>
        public IAsyncEnumerable<DirectoryChangedEventArgs> ReadEventsAsync(CancellationToken token) {
            return _eventChannel.Reader.ReadAllAsync(token);
        }

        /// <summary>
        /// Safely adds or removes watchers to match the incoming configuration.
        /// Expected by DashboardViewModel.SaveConfigurationInternal()
        /// </summary>
        public void SyncWatchers(Dictionary<Guid, WatcherConfig> directoriesToWatch) {
            lock (_lock) {
                // 1. Remove and Dispose of old watchers
                var idsToRemove = _watchers.Keys.Where(id => !directoriesToWatch.ContainsKey(id)).ToList();
                foreach (var id in idsToRemove) {
                    StopAndDisposeWatcher(id);
                }

                // CORRECTED: Only abort if the service is explicitly stopped
                if (!IsRunning) return;

                // 2. Add/Re-add watchers for new configurations
                foreach (var kvp in directoriesToWatch) {
                    if (_watchers.ContainsKey(kvp.Key)) continue;

                    if (!Directory.Exists(kvp.Value.DirectoryPath)) {
                        Logger.Log($"Warning: Cannot watch non-existent directory: {kvp.Value.DirectoryPath}");
                        continue;
                    }

                    Logger.Log($"[Monitoring Started] Directory: {kvp.Value.DirectoryPath} for Plan: {kvp.Value.PlanName}");

                    var watcher = new FileSystemWatcher(kvp.Value.DirectoryPath) {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                        IncludeSubdirectories = true,
                        InternalBufferSize = 65536
                    };

                    watcher.Changed += (s, e) => OnFileSystemEvent(kvp.Key, kvp.Value, e);
                    watcher.Created += (s, e) => OnFileSystemEvent(kvp.Key, kvp.Value, e);
                    watcher.Deleted += (s, e) => OnFileSystemEvent(kvp.Key, kvp.Value, e);
                    watcher.Renamed += (s, e) => OnFileRenamed(kvp.Key, kvp.Value, e);
                    watcher.Error += (s, e) => Logger.Log($"[WATCHER OVERFLOW] Plan: {kvp.Value.PlanName} | {e.GetException().Message}");

                    watcher.EnableRaisingEvents = true; // enable only after all handlers are wired
                    _watchers.Add(kvp.Key, watcher);
                }
            }
        }

        private void OnFileSystemEvent(Guid planId, WatcherConfig config, FileSystemEventArgs e) {
            if (!IsRunning) return;

            string fileKey = $"{e.FullPath}_{e.ChangeType}";
            DateTime now = DateTime.UtcNow;

            lock (_lock) {
                // Debounce logic: swallow rapid-fire VS events
                if (_lastEventTimes.TryGetValue(fileKey, out var lastTime) && (now - lastTime < _debounceThreshold))
                    return;

                _lastEventTimes[fileKey] = now;
            }

            // NEW: Clear, formatted log of the successfully caught event
            Logger.Log($"[CAUGHT EVENT] Plan: {config.PlanName} | File: {e.Name} | Action: {e.ChangeType}");

            // Write to channel
            var changeEvent = new DirectoryChangedEventArgs(planId, e.Name ?? string.Empty, e.ChangeType.ToString());
            _eventChannel.Writer.TryWrite(changeEvent);
        }

        private void OnFileRenamed(Guid planId, WatcherConfig config, RenamedEventArgs e) {
            if (!IsRunning) return;

            string fileKey = $"{e.FullPath}_Renamed";
            DateTime now = DateTime.UtcNow;

            lock (_lock) {
                if (_lastEventTimes.TryGetValue(fileKey, out var lastTime) && (now - lastTime < _debounceThreshold))
                    return;

                _lastEventTimes[fileKey] = now;
            }

            Logger.Log($"[CAUGHT EVENT] Plan: {config.PlanName} | File: {e.OldName} -> {e.Name} | Action: Renamed");

            var changeEvent = new DirectoryChangedEventArgs(planId, e.Name ?? string.Empty, "Renamed", e.OldName);
            _eventChannel.Writer.TryWrite(changeEvent);
        }

        private void StopAndDisposeWatcher(Guid id) {
            if (_watchers.TryGetValue(id, out var watcher)) {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose(); // Immediately releases the OS handle and detached lambdas
                _watchers.Remove(id);
            }
        }

        public void Dispose() {
            Stop();
            _eventChannel.Writer.TryComplete();
            GC.SuppressFinalize(this);
        }
    }
}