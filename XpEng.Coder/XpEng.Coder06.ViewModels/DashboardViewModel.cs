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

        #region Watcher
        private DirectoriesWatcher? _watcher;
        private readonly Lock _watcherLock = new();
        public DirectoriesWatcher Watcher {
            get {
                if (_watcher != null) return _watcher;
                lock (_watcherLock) {
                    _watcher ??= new DirectoriesWatcher();
                }
                return _watcher;
            }
            set { lock (_watcherLock) { _watcher = value; } }
        }
        #endregion Watcher

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
                    WireSourceDirectories(poco);
                }
            }
            if (e.OldItems != null) {
                foreach (PlanOrchestratorPoco poco in e.OldItems) {
                    poco.PropertyChanged -= Poco_PropertyChanged;
                    UnwireSourceDirectories(poco);
                }
            }
            TriggerAutoSave();
        }

        private void WireSourceDirectories(PlanOrchestratorPoco plan) {
            plan.SourceDirectories.CollectionChanged += OnSourceDirectoriesChanged;
            foreach (SourceDirectoryPoco source in plan.SourceDirectories) {
                source.PropertyChanged += Poco_PropertyChanged;
                WireTargetTemplates(source);
            }
        }

        private void UnwireSourceDirectories(PlanOrchestratorPoco plan) {
            plan.SourceDirectories.CollectionChanged -= OnSourceDirectoriesChanged;
            foreach (SourceDirectoryPoco source in plan.SourceDirectories) {
                source.PropertyChanged -= Poco_PropertyChanged;
                UnwireTargetTemplates(source);
            }
        }

        private void OnSourceDirectoriesChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (SourceDirectoryPoco poco in e.NewItems) {
                    poco.PropertyChanged += Poco_PropertyChanged;
                    WireTargetTemplates(poco);
                }
            }
            if (e.OldItems != null) {
                foreach (SourceDirectoryPoco poco in e.OldItems) {
                    poco.PropertyChanged -= Poco_PropertyChanged;
                    UnwireTargetTemplates(poco);
                }
            }
            EnsureBlankRows();
            TriggerAutoSave();
        }

        private void WireTargetTemplates(SourceDirectoryPoco source) {
            source.TargetTemplates.CollectionChanged += OnTargetTemplatesChanged;
            foreach (TargetTemplatePoco target in source.TargetTemplates) {
                target.PropertyChanged += Poco_PropertyChanged;
            }
        }

        private void UnwireTargetTemplates(SourceDirectoryPoco source) {
            source.TargetTemplates.CollectionChanged -= OnTargetTemplatesChanged;
            foreach (TargetTemplatePoco target in source.TargetTemplates) {
                target.PropertyChanged -= Poco_PropertyChanged;
            }
        }

        private void OnTargetTemplatesChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (TargetTemplatePoco poco in e.NewItems) {
                    poco.PropertyChanged += Poco_PropertyChanged;
                }
            }
            if (e.OldItems != null) {
                foreach (TargetTemplatePoco poco in e.OldItems) {
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

        private bool CanToggleWatch => IsWatching || UIPlans.Any(p => p.SourceDirectories.Any(s => s.TargetTemplates.Any(t => !t.IsEmpty)));
        private bool CanForceSync => !IsWatching && UIPlans.Any(p => p.SourceDirectories.Any(s => s.TargetTemplates.Any(t => !t.IsEmpty)));

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
                if (!plan.SourceDirectories.Any() || !plan.SourceDirectories.Last().IsEmpty) {
                    plan.SourceDirectories.Add(new SourceDirectoryPoco());
                }

                foreach (var source in plan.SourceDirectories) {
                    if (!source.TargetTemplates.Any() || !source.TargetTemplates.Last().IsEmpty) {
                        source.TargetTemplates.Add(new TargetTemplatePoco());
                    }
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
        private void DeleteSourceDirectory(SourceDirectoryPoco? sourcePoco) {
            if (sourcePoco != null && !sourcePoco.IsEmpty) {
                var parentPlan = UIPlans.FirstOrDefault(p => p.SourceDirectories.Contains(sourcePoco));
                parentPlan?.SourceDirectories.Remove(sourcePoco);
                EnsureBlankRows();
                TriggerAutoSave();
            }
        }

        [RelayCommand]
        private void DeleteTargetTemplate(TargetTemplatePoco? targetPoco) {
            if (targetPoco != null && !targetPoco.IsEmpty) {
                var parentSource = UIPlans.SelectMany(p => p.SourceDirectories).FirstOrDefault(s => s.TargetTemplates.Contains(targetPoco));
                parentSource?.TargetTemplates.Remove(targetPoco);
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

            // 1. Enforce structural validity first, so a Plan with a blank name (which fails
            // [Required] on PlanName) is reported as "name required" rather than surfacing
            // as a confusing duplicate-name collision further down.
            if (plansToSave.Any(p => !p.IsStructurallyValid())) {
                if (!isAutoSave) {
                    Logger.Log("Save failed: One or more plans are not structurally valid (check required fields).");
                }
                return;
            }

            // 2. Enforce Uniqueness
            var duplicateNames = plansToSave
                .GroupBy(p => p.PlanName?.Trim().ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Any()) {
                if (!isAutoSave) {
                    var displayNames = duplicateNames.Select(n => string.IsNullOrEmpty(n) ? "(blank)" : n);
                    Logger.Log($"Save failed: Plan names must be unique. Duplicates found: {string.Join(", ", displayNames)}");
                }
                return; // Abort the save
            }

            var newDomainModels = new System.Collections.Generic.List<PlanOrchestrator>();
            foreach (var poco in plansToSave) {
                var cleanPoco = new PlanOrchestratorPoco {
                    Id = poco.Id,
                    PlanName = poco.PlanName,
                    IsMonitored = poco.IsMonitored,
                    SourceDirectories = new ObservableCollection<SourceDirectoryPoco>(
                        poco.SourceDirectories.Where(s => !s.IsEmpty).Select(s => new SourceDirectoryPoco {
                            Id = s.Id,
                            SourceDirectory = s.SourceDirectory,
                            TargetTemplates = new ObservableCollection<TargetTemplatePoco>(s.TargetTemplates.Where(t => !t.IsEmpty))
                        }))
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
            // Keyed by SourceDirectory.Id (not Plan.Id): each Source Directory gets its own
            // FileSystemWatcher, since a Plan may now own multiple Source Directories.
            return MainModel.Instance.PlanOrchestrators
                .Where(p => p.IsMonitored)
                .SelectMany(p => p.SourceDirectories
                    .Where(s => s.SourcePath != null && s.SourcePath.Exists && s.TargetTemplates.Any(t => t.IsMonitored))
                    .Select(s => new { s.Id, Config = new WatcherConfig(s.SourcePath.FullName, p.PlanName ?? "Unknown Plan") }))
                .ToDictionary(x => x.Id, x => x.Config);
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

        // Finds which Plan owns the Source Directory that raised the watcher event,
        // since a Source Directory's Id (not a Plan's Id) is now the watcher key.
        private (PlanOrchestrator? Plan, SourceDirectory? Source) FindSourceDirectory(Guid watcherId) {
            foreach (var plan in MainModel.Instance.PlanOrchestrators) {
                var source = plan.SourceDirectories.FirstOrDefault(s => s.Id == watcherId);
                if (source != null) return (plan, source);
            }
            return (null, null);
        }

        private async Task ConsumeWatcherEventsAsync(CancellationToken token) {
            try {
                await foreach (var changeEvent in Watcher.ReadEventsAsync(token)) {
                    try {
                        // 1. ABSOLUTE TOP: Log the raw event the microsecond it hits the channel
                        Logger.Log($"Raw Event: [{changeEvent.ChangeType}] {changeEvent.FileName} (WatcherID: {changeEvent.WatcherId})");

                        var (affectedPlan, affectedSource) = FindSourceDirectory(changeEvent.WatcherId);

                        // 2. Expose the mismatch!
                        if (affectedPlan == null || affectedSource == null) {
                            Logger.Log($"WARNING: WatcherId {changeEvent.WatcherId} does not match any active Source Directory!");
                            continue;
                        }

                        string fullFilePath = Path.Combine(affectedSource.SourcePath.FullName, changeEvent.FileName ?? string.Empty);

                        // 3. STRICT FILTER: only actively-monitored target/template pairs
                        var activeTargets = affectedSource.TargetTemplates
                            .Where(t => t.IsMonitored)
                            .Select(t => new GenerationTarget(t.TemplatePath.FullName, t.TargetDirectory.FullName))
                            .ToList();

                        if (!activeTargets.Any()) {
                            Logger.Log($"Skipping: No active targets configured for plan '{affectedPlan.PlanName}'.");
                            continue;
                        }

                        string? oldFilePath = string.IsNullOrEmpty(changeEvent.OldFileName) ? null : Path.Combine(affectedSource.SourcePath.FullName, changeEvent.OldFileName);

                        var caller = DIExtensions.ServiceProvider.GetRequiredService<ITemplatesCaller>();
                        await caller.ProcessFileAsync(affectedPlan.PlanName, activeTargets, fullFilePath, changeEvent.ChangeType, oldFilePath);
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
            _watcher?.Dispose();
            _watchCancellationTokenSource?.Cancel();
            _watchCancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Event Handlers & Methods
    }
}