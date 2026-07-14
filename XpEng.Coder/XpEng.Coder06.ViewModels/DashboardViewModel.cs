using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using XpEng.Coder12.Services;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder06.ViewModels {
    public class DashboardViewModel : ViewModelBase, IDisposable {
        private ICodeGenerator Generator => field ??= new CodeGenerator();

        private IConfigurationService ConfigService => field ??= new JsonConfigurationService();

        private SynchronizationContext SyncContext => field ??= SynchronizationContext.Current ?? new SynchronizationContext();

        private DirectoryWatcher? _watcher;

        private AppConfig CurrentConfig {
            get {
                if (field == null) {
                    field = ConfigService.Load();
                    // We no longer auto-inject UIConstants here. 
                    // If the JSON is empty, the properties remain empty strings.
                }
                return field;
            }
        }

        private bool HasValidPaths =>
            !string.IsNullOrWhiteSpace(SourceDirectory) &&
            !string.IsNullOrWhiteSpace(TargetDirectory) &&
            !string.IsNullOrWhiteSpace(TemplatePath);

        public string SourceDirectory {
            get => CurrentConfig.SourceDirectory;
            set {
                if (CurrentConfig.SourceDirectory != value) {
                    CurrentConfig.SourceDirectory = value;
                    OnPropertyChanged();
                    NotifyCommands();
                    SaveConfiguration();
                }
            }
        }

        public string TargetDirectory {
            get => CurrentConfig.TargetDirectory;
            set {
                if (CurrentConfig.TargetDirectory != value) {
                    CurrentConfig.TargetDirectory = value;
                    OnPropertyChanged();
                    NotifyCommands();
                    SaveConfiguration();
                }
            }
        }

        public string TemplatePath {
            get => CurrentConfig.TemplatePath;
            set {
                string finalPath = value;

                if (!string.IsNullOrWhiteSpace(value) && System.IO.Directory.Exists(value)) {
                    var inferred = InferTemplateFile(value);
                    if (inferred != null) {
                        finalPath = inferred;
                    }
                }

                if (CurrentConfig.TemplatePath != finalPath) {
                    CurrentConfig.TemplatePath = finalPath;
                    OnPropertyChanged();
                    NotifyCommands();
                    SaveConfiguration();
                }
            }
        }

        public bool IsWatching {
            get => field;
            set {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToggleButtonText));
                NotifyCommands();
            }
        }

        public string ToggleButtonText => IsWatching ? UIConstants.StopWatchingAction : UIConstants.StartWatchingAction;

        public ObservableCollection<string> LiveLogs => field ??= InitializeLogs();

        // Commands now check HasValidPaths before allowing execution
        public ICommand ToggleWatchCommand => field ??= new RelayCommand(ToggleWatching, () => HasValidPaths || IsWatching);

        public ICommand ForceSyncCommand => field ??= new RelayCommand(ForceSync, () => !IsWatching && HasValidPaths);


        public DashboardViewModel() {
        }

        private ObservableCollection<string> InitializeLogs() {
            return new ObservableCollection<string>
            {
                $"[{DateTime.Now:HH:mm:ss}] Engine initialized. Configuration loaded."
            };
        }

        private void SaveConfiguration() {
            // Capture a local reference to safely pass into the background thread
            var configToSave = CurrentConfig;
            System.Threading.Tasks.Task.Run(() => {
                ConfigService.Save(configToSave);
            });
        }

        private string? InferTemplateFile(string directory) {
            string[] extensions = { "*.tt", "*.csx", "*.cs" };
            foreach (var ext in extensions) {
                var files = System.IO.Directory.GetFiles(directory, ext);
                if (files.Length > 0) {
                    return files[0];
                }
            }
            return null;
        }

        private void NotifyCommands() {
            // If your RelayCommand uses CommandManager.RequerySuggested, this might be redundant,
            // but explicitly notifying the UI that command states may have changed is best practice.
            OnPropertyChanged(nameof(ToggleWatchCommand));
            OnPropertyChanged(nameof(ForceSyncCommand));
        }

        private void ToggleWatching() {
            if (IsWatching) {
                _watcher?.Stop();
                _watcher?.Dispose();
                _watcher = null;
                IsWatching = false;
                LogToUi(UIConstants.SystemOfflineMsg);
            }
            else {
                _watcher = new DirectoryWatcher(SourceDirectory, TargetDirectory, Generator, LogToUi);
                _watcher.Start();
                IsWatching = true;
            }
        }

        private void ForceSync() {
            LogToUi(UIConstants.ForceSyncAction + " initiated...");
            System.Threading.Tasks.Task.Run(() => {
                Generator.FullSynchronization(SourceDirectory, TargetDirectory, LogToUi);
            });
        }

        private void LogToUi(string message) {
            SyncContext.Post(_ => {
                LiveLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            }, null);
        }

        public void Dispose() {
            if (_watcher != null) {
                _watcher.Stop();
                _watcher.Dispose();
                _watcher = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}