using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using XpEng.Coder09.Models;
using XpEng.Coder09.Models.Entities;
using XpEng.Coder09.Models.Transport;
using XpEng.Coder12.Services;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services; 

namespace XpEng.Coder06.ViewModels {

    public partial class DashboardViewModel : ViewModelBase, IDisposable {

        #region Properties
        private IEngineLogger Logger => DIExtensions.ServiceProvider.GetRequiredService<IEngineLogger>();
        private DirectoriesWatcher Watcher => new ();
        private CancellationTokenSource? _watchCancellationTokenSource;
        private bool _isUpdatingLayout = false;
        private bool _isInitializing = true;

        [ObservableProperty]
        private bool _autoSave = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
        [NotifyCanExecuteChangedFor(nameof(ToggleWatchCommand))]
        [NotifyCanExecuteChangedFor(nameof(ForceSyncCommand))]
        private bool _isWatching;

        public string ToggleButtonText => IsWatching ? UIConstants.StopWatchingAction : UIConstants.StartWatchingAction;

        // Bind directly to the Infrastructure Logger's live collection
        public ObservableCollection<XpEng.Coder80.Infrastructure.Services.LogItem> LiveLogs => Logger.LiveLogs;

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
            if (_isInitializing || !AutoSave) return;
            SaveConfigurationInternal(isAutoSave: true);
        }

        [RelayCommand]
        private void SaveConfiguration() {
            SaveConfigurationInternal(isAutoSave: false);
        }

        private void SaveConfigurationInternal(bool isAutoSave) {
            var plansToSave = UIPlans.Where(p => !p.IsEmpty).ToList();

            // 1. Enforce Uniqueness
            var duplicateNames = plansToSave
                .GroupBy(p => p.PlanName?.Trim().ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Any()) {
                if (!isAutoSave) {
                    Logger.Log($"Save failed: Plan names must be unique. Duplicates found: {string.Join(", ", duplicateNames)}");
                }
                return; // Abort the save
            }

            if (plansToSave.Any(p => !p.IsStructurallyValid())) return;

            var newDomainModels = new System.Collections.Generic.List<PlanOrchestrator>();
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

            if (!isAutoSave) {
                Logger.Log("Configuration saved explicitly.");
                NotifyCommands();
            }

            // ONLY push to the OS watchers if we are actively monitoring
            if (IsWatching) {
                Watcher.SyncWatchers(GetCurrentWatcherConfig());
            }
        }

        private Dictionary<Guid, WatcherConfig> GetCurrentWatcherConfig() {
            // TIER BRIDGE: Extract pure primitives so Tier 12 knows nothing about Tier 09 Models
            return MainModel.Instance.PlanOrchestrators
                .Where(p => p.SourceDirectory != null && p.SourceDirectory.Exists && p.TemplateTargets.Any(t => t.IsMonitored))
                .ToDictionary(
                    p => p.Id,
                    p => new WatcherConfig(p.SourceDirectory!.FullName, p.PlanName ?? "Unknown Plan")
                );
        }

        [RelayCommand(CanExecute = nameof(CanToggleWatch))]
        private void ToggleWatch() {
            if (IsWatching) {
                // Use Stop()! This sets IsRunning = false and kills the OS handles.
                Watcher.Stop();

                _watchCancellationTokenSource?.Cancel();
                IsWatching = false;
                Logger.Log("Monitoring task suspended.");
            }
            else {
                SaveConfigurationInternal(isAutoSave: true);

                var configToWatch = GetCurrentWatcherConfig();

                // THE FIX: Use Start() so it sets IsRunning = true BEFORE syncing!
                Watcher.Start(configToWatch);

                _watchCancellationTokenSource = new CancellationTokenSource();
                _ = ConsumeWatcherEventsAsync(_watchCancellationTokenSource.Token);

                IsWatching = true;
                Logger.Log("DirectoriesWatcher service started.");
            }
        }

        private async Task ConsumeWatcherEventsAsync(CancellationToken token) {
            try {
                await foreach (var changeEvent in Watcher.ReadEventsAsync(token)) {
                    try {
                        // 1. ABSOLUTE TOP: Log the raw event the microsecond it hits the channel
                        Logger.Log($"Raw Event: [{changeEvent.ChangeType}] {changeEvent.FileName} (WatcherID: {changeEvent.WatcherId})");

                        var affectedPlan = MainModel.Instance.PlanOrchestrators
                            .FirstOrDefault(p => p.Id == changeEvent.WatcherId);

                        // 2. Expose the mismatch!
                        if (affectedPlan == null) {
                            Logger.Log($"WARNING: WatcherId {changeEvent.WatcherId} does not match any active Plan Orchestrator ID!");
                            continue;
                        }

                        if (affectedPlan.SourceDirectory == null) {
                            Logger.Log($"Error: Source directory is null for plan '{affectedPlan.PlanName}'.");
                            continue;
                        }

                        string fullFilePath = Path.Combine(affectedPlan.SourceDirectory.FullName, changeEvent.FileName ?? string.Empty);

                        // 3. STRICT NULL CHECKS: Prevents empty UI rows from crashing the loop
                        var activeTargets = affectedPlan.TemplateTargets
                            .Where(t => t.IsMonitored && t.TemplatePath != null && t.TargetDirectory != null)
                            .Select(t => new GenerationTarget(t.TemplatePath!.FullName, t.TargetDirectory!.FullName))
                            .ToList();

                        if (!activeTargets.Any()) {
                            Logger.Log($"Skipping: No active targets configured for plan '{affectedPlan.PlanName}'.");
                            continue;
                        }

                        var caller = DIExtensions.ServiceProvider.GetRequiredService<ITemplatesCaller>();
                        await caller.ProcessFileAsync(affectedPlan.PlanName, activeTargets, fullFilePath, changeEvent.ChangeType);
                    }
                    catch (Exception ex) {
                        Logger.Log($"Processing error for '{changeEvent.FileName}': {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) {
                Logger.Log("Operation cancelled. Monitoring task suspended. ");
            }
            catch (Exception fatalEx) {
                Logger.Log($"FATAL CHANNEL ERROR: {fatalEx.Message}");
            }
        }

        [RelayCommand]
        private void CopyLogs() {
            var selectedLogs = LiveLogs.Where(log => log.IsSelected).Select(log => log.Message).ToList();
            if (selectedLogs.Any()) {
                CopyToClipboardRequested?.Invoke(this, string.Join(Environment.NewLine, selectedLogs));
            }
        }

        [RelayCommand(CanExecute = nameof(CanForceSync))]
        private async Task ForceSync() {
            // Existing Force Sync Logic 
        }

        private void NotifyCommands() {
            ToggleWatchCommand?.NotifyCanExecuteChanged();
            ForceSyncCommand?.NotifyCanExecuteChanged();
        }

        public void Dispose() {
            _watchCancellationTokenSource?.Cancel();
            _watchCancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Event Handlers & Methods
    }
}