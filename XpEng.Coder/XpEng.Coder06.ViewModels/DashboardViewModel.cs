using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks; // Added so 'Task' is recognized
using System.Windows.Input;
using XpEng.Coder12.Services;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder06.ViewModels {

    // ADDED 'partial' modifier: Required by CommunityToolkit.Mvvm for [RelayCommand] to work
    public partial class DashboardViewModel : ViewModelBase, IDisposable {

        private ICodeGenerator Generator => field ??= new CodeGenerator();

        private IConfigurationService ConfigService => field ??= new JsonConfigurationService();

        private SynchronizationContext SyncContext => field ??= SynchronizationContext.Current ?? new SynchronizationContext();

        private DirectoryWatcher? _watcher;

        private bool CanForceSync => !IsWatching && HasValidPaths;
        private bool CanToggleWatch => HasValidPaths || IsWatching;

        private AppConfig CurrentConfig {
            get {
                if (field == null) {
                    field = ConfigService.Load();
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

        public DashboardViewModel() {
        }

        private ObservableCollection<string> InitializeLogs() {
            return new ObservableCollection<string>
            {
                $"[{DateTime.Now:HH:mm:ss}] Engine initialized. Configuration loaded."
            };
        }

        private void SaveConfiguration() {
            var configToSave = CurrentConfig;
            Task.Run(() => {
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
            // This safely tells the generated commands to re-evaluate their CanExecute properties
            ToggleWatchCommand?.NotifyCanExecuteChanged();
            ForceSyncCommand?.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanToggleWatch))]
        private void ToggleWatch() {
            if (IsWatching) {
                _watcher?.Stop();
                _watcher?.Dispose();
                _watcher = null;
                IsWatching = false;
                LogToUi(UIConstants.SystemOfflineMsg);
            }
            else {
                _watcher = new DirectoryWatcher(SourceDirectory, TargetDirectory, TemplatePath, Generator, LogToUi);
                _watcher.Start();
                IsWatching = true;
            }
        }

        [RelayCommand(CanExecute = nameof(CanForceSync))]
        private async Task ForceSync() {
            if (!HasValidPaths) {
                LogToUi(UIConstants.ErrorMissingPathsMsg);
                return;
            }

            await Generator.FullSynchronizationAsync(SourceDirectory, TargetDirectory, TemplatePath, LogToUi);
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