using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using XpEng.Coder80.Infrastructure;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;
// Add your other required using statements for WatcherConfig and DirectoryChangedEventArgs

namespace XpEng.Coder12.Services {
    public class DirectoriesWatcher : IDisposable {
        private readonly Dictionary<Guid, FileSystemWatcher> _watchers = new();
        private readonly Dictionary<string, DateTime> _lastEventTimes = new();
        private readonly TimeSpan _debounceThreshold = TimeSpan.FromMilliseconds(50);
        private readonly object _lock = new();

        // Trailing-edge debounce: events for a given Source Directory accumulate here.
        // Each new event resets that Source Directory's timer; the batch only flushes
        // once 300ms pass with no further activity for it.
        private readonly Dictionary<Guid, List<DirectoryChangedEventArgs>> _pendingBatches = new();
        private readonly Dictionary<Guid, CancellationTokenSource> _debounceTokenSources = new();
        private readonly TimeSpan _debounceWindow = TimeSpan.FromMilliseconds(300);

        // Channel for asynchronous event streaming to the ViewModel. Each item is a whole
        // batch of changes for one Source Directory, not a single raw file-system event.
        private readonly Channel<List<DirectoryChangedEventArgs>> _eventChannel = Channel.CreateUnbounded<List<DirectoryChangedEventArgs>>();

        // Dynamically resolve logger exactly like the ViewModel does
        private IEngineLogger Logger => ApplicationServices.Provider.GetRequiredService<IEngineLogger>();

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

                // Cancel any in-flight debounce timers and discard unflushed batches —
                // monitoring is stopping, so pending changes are no longer relevant.
                foreach (var cts in _debounceTokenSources.Values) {
                    cts.Cancel();
                    cts.Dispose();
                }
                _debounceTokenSources.Clear();
                _pendingBatches.Clear();
            }
        }

        /// <summary>
        /// Exposes the channel reader as an IAsyncEnumerable.
        /// Expected by DashboardViewModel.ConsumeWatcherEventsAsync()
        /// </summary>
        public IAsyncEnumerable<List<DirectoryChangedEventArgs>> ReadEventsAsync(CancellationToken token) {
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

        private void OnFileSystemEvent(Guid sourceDirectoryId, WatcherConfig config, FileSystemEventArgs e) {
            if (!IsRunning) return;

            string fileKey = $"{e.FullPath}_{e.ChangeType}";
            DateTime now = DateTime.UtcNow;

            lock (_lock) {
                // Debounce logic: swallow rapid-fire duplicate raw events of the SAME type
                if (_lastEventTimes.TryGetValue(fileKey, out var lastTime) && (now - lastTime < _debounceThreshold))
                    return;

                _lastEventTimes[fileKey] = now;
            }

            Logger.Log($"[CAUGHT EVENT] Plan: {config.PlanName} | File: {e.Name} | Action: {e.ChangeType}");

            var changeEvent = new DirectoryChangedEventArgs(sourceDirectoryId, e.Name ?? string.Empty, e.ChangeType.ToString());
            EnqueueForBatch(sourceDirectoryId, changeEvent);
        }

        private void OnFileRenamed(Guid sourceDirectoryId, WatcherConfig config, RenamedEventArgs e) {
            if (!IsRunning) return;

            string fileKey = $"{e.FullPath}_Renamed";
            DateTime now = DateTime.UtcNow;

            lock (_lock) {
                if (_lastEventTimes.TryGetValue(fileKey, out var lastTime) && (now - lastTime < _debounceThreshold))
                    return;

                _lastEventTimes[fileKey] = now;
            }

            Logger.Log($"[CAUGHT EVENT] Plan: {config.PlanName} | File: {e.OldName} -> {e.Name} | Action: Renamed");

            var changeEvent = new DirectoryChangedEventArgs(sourceDirectoryId, e.Name ?? string.Empty, "Renamed", e.OldName);
            EnqueueForBatch(sourceDirectoryId, changeEvent);
        }

        // Accumulates one event into its Source Directory's pending batch, then cancels
        // and restarts that Source Directory's 300ms flush timer — trailing-edge debounce.
        private void EnqueueForBatch(Guid sourceDirectoryId, DirectoryChangedEventArgs changeEvent) {
            lock (_lock) {
                if (!_pendingBatches.TryGetValue(sourceDirectoryId, out var batch)) {
                    batch = new List<DirectoryChangedEventArgs>();
                    _pendingBatches[sourceDirectoryId] = batch;
                }
                batch.Add(changeEvent);

                if (_debounceTokenSources.TryGetValue(sourceDirectoryId, out var existingCts)) {
                    existingCts.Cancel();
                    existingCts.Dispose();
                }

                var cts = new CancellationTokenSource();
                _debounceTokenSources[sourceDirectoryId] = cts;

                _ = Task.Delay(_debounceWindow, cts.Token).ContinueWith(t => {
                    FlushBatch(sourceDirectoryId);
                }, cts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
        }

        private void FlushBatch(Guid sourceDirectoryId) {
            List<DirectoryChangedEventArgs>? batchToFlush = null;

            lock (_lock) {
                if (_pendingBatches.TryGetValue(sourceDirectoryId, out var batch) && batch.Count > 0) {
                    batchToFlush = batch;
                    _pendingBatches[sourceDirectoryId] = new List<DirectoryChangedEventArgs>();
                }
                _debounceTokenSources.Remove(sourceDirectoryId);
            }

            if (batchToFlush != null) {
                _eventChannel.Writer.TryWrite(batchToFlush);
            }
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