using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using XpEng.Coder09.Models.Entities;
using XpEng.Coder12.Services;

namespace XpEng.Coder06.ViewModels {

    public partial class DashboardViewModel : ViewModelBase, IDisposable {

        #region Services & Infrastructure
        private ITemplatesCaller Generator => field ??= new TemplatesCaller();

        private SynchronizationContext SyncContext => field ??= SynchronizationContext.Current ?? new SynchronizationContext();

        private DirectoryWatcher? _watcher;
        #endregion

        #region UI State & Collections
        public ObservableCollection<LogItem> LiveLogs => field ??= InitializeLogs();

        // 1. The new Hierarchical UI Collection
        public ObservableCollection<PlanOrchestratorViewModel> PlanViewModels => field ??= InitializePlanViewModels();

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

        public event EventHandler<string>? CopyToClipboardRequested;
        #endregion

        #region Command Rules
        // A watcher can start if there is at least one plan with one target
        private bool CanToggleWatch => IsWatching || PlanViewModels.Any(p => p.Model.TemplateTargets.Any());
        private bool CanForceSync => !IsWatching && PlanViewModels.Any(p => p.Model.TemplateTargets.Any());
        #endregion

        public DashboardViewModel() { }

        #region Initialization
        private ObservableCollection<LogItem> InitializeLogs() {
            return new ObservableCollection<LogItem>
            {
                new LogItem($"[{DateTime.Now:HH:mm:ss}] Engine initialized. Configuration loaded.")
            };
        }

        private ObservableCollection<PlanOrchestratorViewModel> InitializePlanViewModels() {
            var vms = new ObservableCollection<PlanOrchestratorViewModel>();

            // Hydrate the UI wrappers from the MainModel
            foreach (var plan in MainModel.PlanOrchestrators) {
                vms.Add(new PlanOrchestratorViewModel(plan));
            }
            return vms;
        }
        #endregion

        #region Plan Commands (New)
        [RelayCommand]
        private void AddPlan() {
            var newPlan = new PlanOrchestrator("New Unnamed Plan", new DirectoryInfo(@"C:\DefaultSource"));
            MainModel.PlanOrchestrators.Add(newPlan);

            var planVm = new PlanOrchestratorViewModel(newPlan) { IsExpanded = true };
            PlanViewModels.Add(planVm);

            NotifyCommands();
        }

        [RelayCommand]
        private void DeletePlan(PlanOrchestratorViewModel? planVm) {
            if (planVm != null) {
                MainModel.PlanOrchestrators.Remove(planVm.Model);
                PlanViewModels.Remove(planVm);
                NotifyCommands();
            }
        }
        #endregion

        #region Execution Commands (Legacy Migrated)
        [RelayCommand]
        private void CopyLogs() {
            var selectedLogs = LiveLogs
                .Where(log => log.IsSelected)
                .Select(log => log.Message).ToList();

            if (!selectedLogs.Any()) return;
            var textToCopy = string.Join(Environment.NewLine, selectedLogs);

            CopyToClipboardRequested?.Invoke(this, textToCopy);
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
                // TODO: DirectoryWatcher needs an update to accept the MainModel.PlanOrchestrators collection 
                // instead of a single Source/Target/Template string.

                // _watcher = new DirectoryWatcher(MainModel.PlanOrchestrators, Generator, LogToUi);
                // _watcher.Start();

                IsWatching = true;
                LogToUi("System Online. Monitoring multiple plans...");
            }
        }

        [RelayCommand(CanExecute = nameof(CanForceSync))]
        private async Task ForceSync() {
            var activePlans = MainModel.PlanOrchestrators.Where(p => p.IsMonitored).ToList();

            if (!activePlans.Any()) {
                LogToUi("No active plans monitored for synchronization.");
                return;
            }

            // TODO: ITemplatesCaller needs to loop through the valid TemplateTargets 
            // inside the active Plans, rather than taking a single set of strings.

            // await Generator.FullSynchronizationAsync(activePlans, LogToUi);
        }

        private void NotifyCommands() {
            ToggleWatchCommand?.NotifyCanExecuteChanged();
            ForceSyncCommand?.NotifyCanExecuteChanged();
        }

        private void LogToUi(string message) {
            SyncContext.Post(_ => {
                LiveLogs.Add(new LogItem($"[{DateTime.Now:HH:mm:ss}] {message}"));
            }, null);
        }
        #endregion

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