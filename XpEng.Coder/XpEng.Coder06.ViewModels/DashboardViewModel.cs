using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XpEng.Coder09.Models;
using XpEng.Coder09.Models.Entities;
using XpEng.Coder09.Models.Transport;
using XpEng.Coder12.Services;

namespace XpEng.Coder06.ViewModels {

    public partial class DashboardViewModel : ViewModelBase, IDisposable {

        #region Properties
        private ITemplatesCaller Generator => field ??= new TemplatesCaller();
        private SynchronizationContext SyncContext => field ??= SynchronizationContext.Current ?? new SynchronizationContext();
        private DirectoryWatcher? _watcher;
        private bool _isUpdatingLayout = false;

        // NEW: Prevents AutoSave from nuking the JSON file while the UI is loading
        private bool _isInitializing = true;

        [ObservableProperty]
        private bool _autoSave = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
        [NotifyCanExecuteChangedFor(nameof(ToggleWatchCommand))]
        [NotifyCanExecuteChangedFor(nameof(ForceSyncCommand))]
        private bool _isWatching;

        public string ToggleButtonText => IsWatching ? UIConstants.StopWatchingAction : UIConstants.StartWatchingAction;

        public ObservableCollection<LogItem> LiveLogs => field ??= InitializeLogs();

        #region UIPlans
        public ObservableCollection<PlanOrchestratorPoco> UIPlans {
            get {
                if (field == null) {
                    field = new ObservableCollection<PlanOrchestratorPoco>();
                    field.CollectionChanged += OnUIPlansChanged;
                }
                return field;
            }
        }

        private void OnUIPlansChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (PlanOrchestratorPoco poco in e.NewItems) {
                    poco.PropertyChanged += Poco_PropertyChanged;
                    poco.TemplateTargets.CollectionChanged += OnTargetsChanged;
                }
            }
            if (e.OldItems != null) {
                foreach (PlanOrchestratorPoco poco in e.OldItems) {
                    poco.PropertyChanged -= Poco_PropertyChanged;
                    poco.TemplateTargets.CollectionChanged -= OnTargetsChanged;
                }
            }
            TriggerAutoSave();
        }

        private void OnTargetsChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (TemplateTargetPoco poco in e.NewItems) {
                    poco.PropertyChanged += Poco_PropertyChanged;
                }
            }
            if (e.OldItems != null) {
                foreach (TemplateTargetPoco poco in e.OldItems) {
                    poco.PropertyChanged -= Poco_PropertyChanged;
                }
            }
            EnsureBlankRows();
            TriggerAutoSave();
        }

        private void Poco_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
            EnsureBlankRows();
            TriggerAutoSave();
        }
        #endregion UIPlans

        private bool CanToggleWatch => IsWatching || UIPlans.Any(p => p.TemplateTargets.Any(t => !t.IsEmpty));
        private bool CanForceSync => !IsWatching && UIPlans.Any(p => p.TemplateTargets.Any(t => !t.IsEmpty));

        public event EventHandler<string>? CopyToClipboardRequested;
        #endregion Properties

        #region Constructors
        public DashboardViewModel() {
            _isInitializing = true;
            MainModel.Instance.LoadPlans();
            LoadPocosFromDomain();
            _isInitializing = false;
        }
        #endregion Constructors

        #region Event Handlers & Methods
        private void LoadPocosFromDomain() {
            UIPlans.Clear();
            foreach (var plan in MainModel.Instance.PlanOrchestrators.ToList()) {
                UIPlans.Add(plan.ToPoco());
            }
            EnsureBlankRows();
        }

        private void EnsureBlankRows() {
            if (_isUpdatingLayout) return;
            _isUpdatingLayout = true;

            if (!UIPlans.Any() || !UIPlans.Last().IsEmpty) {
                UIPlans.Add(new PlanOrchestratorPoco());
            }

            foreach (var plan in UIPlans) {
                if (!plan.TemplateTargets.Any() || !plan.TemplateTargets.Last().IsEmpty) {
                    plan.TemplateTargets.Add(new TemplateTargetPoco());
                }
            }

            _isUpdatingLayout = false;
        }

        [RelayCommand]
        private void DeletePlan(PlanOrchestratorPoco? planPoco) {
            if (planPoco != null && !planPoco.IsEmpty) {
                UIPlans.Remove(planPoco);
                EnsureBlankRows();
                TriggerAutoSave();
            }
        }

        [RelayCommand]
        private void DeleteTemplateTarget(TemplateTargetPoco? targetPoco) {
            if (targetPoco != null && !targetPoco.IsEmpty) {
                var parentPlan = UIPlans.FirstOrDefault(p => p.TemplateTargets.Contains(targetPoco));
                parentPlan?.TemplateTargets.Remove(targetPoco);
                EnsureBlankRows();
                TriggerAutoSave();
            }
        }

        private void TriggerAutoSave() {
            // Abort if the UI is just booting up, or if AutoSave is disabled
            if (_isInitializing || !AutoSave) return;

            // Pass true to indicate this is a silent background save
            SaveConfigurationInternal(isAutoSave: true);
        }

        [RelayCommand]
        private void SaveConfiguration() {
            // Pass false so the operator gets visual confirmation when clicking the button
            SaveConfigurationInternal(isAutoSave: false);
        }

        private void SaveConfigurationInternal(bool isAutoSave) {
            var plansToSave = UIPlans.Where(p => !p.IsEmpty).ToList();

            if (plansToSave.Any(p => !p.IsStructurallyValid())) return;

            // Generate new domain models first to ensure no mapping errors crash the save
            var newDomainModels = new List<PlanOrchestrator>();
            foreach (var poco in plansToSave) {
                var cleanPoco = new PlanOrchestratorPoco {
                    Id = poco.Id,
                    PlanName = poco.PlanName,
                    SourceDirectory = poco.SourceDirectory,
                    TemplateTargets = new ObservableCollection<TemplateTargetPoco>(poco.TemplateTargets.Where(t => !t.IsEmpty))
                };

                try {
                    newDomainModels.Add(new PlanOrchestrator(cleanPoco));
                }
                catch { return; }
            }

            MainModel.Instance.PlanOrchestrators.Clear();
            foreach (var model in newDomainModels) {
                MainModel.Instance.PlanOrchestrators.Add(model);
            }

            MainModel.Instance.SavePlans();

            // Only spam the log if the user explicitly clicked the Save button
            if (!isAutoSave) {
                LogToUi("Configuration saved explicitly.");
                NotifyCommands();
            }
        }

        private ObservableCollection<LogItem> InitializeLogs() { return new ObservableCollection<LogItem> { new LogItem($"[{DateTime.Now:HH:mm:ss}] Engine initialized. Configuration loaded.") }; }
        [RelayCommand] private void CopyLogs() { var selectedLogs = LiveLogs.Where(log => log.IsSelected).Select(log => log.Message).ToList(); if (selectedLogs.Any()) CopyToClipboardRequested?.Invoke(this, string.Join(Environment.NewLine, selectedLogs)); }
        [RelayCommand(CanExecute = nameof(CanToggleWatch))] private void ToggleWatch() { /* Implementation unchanged */ }
        [RelayCommand(CanExecute = nameof(CanForceSync))] private async Task ForceSync() { /* Implementation unchanged */ }
        private void NotifyCommands() { ToggleWatchCommand?.NotifyCanExecuteChanged(); ForceSyncCommand?.NotifyCanExecuteChanged(); }
        private void LogToUi(string message) { SyncContext.Post(_ => { LiveLogs.Add(new LogItem($"[{DateTime.Now:HH:mm:ss}] {message}")); }, null); }
        public void Dispose() { if (_watcher != null) { _watcher.Stop(); _watcher.Dispose(); _watcher = null; } GC.SuppressFinalize(this); }
        #endregion Event Handlers & Methods
    }
}