using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;
using XpEng.Coder09.Models.Transport;


namespace XpEng.Coder09.Models.Entities {
    public partial class PlanOrchestrator : ObservableObject {

        #region properties
        public Guid Id { get; }

        [ObservableProperty]
        private string _planName;

        [ObservableProperty]
        private DirectoryInfo _sourceDirectory;

        public bool IsMonitored {
            get => TemplateTargets.Any() && TemplateTargets.All(t => t.IsMonitored);
            set {
                if (IsMonitored == value) return;

                foreach (var target in TemplateTargets) {
                    target.IsMonitored = value;
                }

                OnPropertyChanged(nameof(IsMonitored));
            }
        }

        #region TemplateTargets

        // Removed the '?' and the inline '= new()'
        [ObservableProperty]
        private ObservableCollection<TemplateTarget> _templateTargets;

        // Toolkit Method: Fires BEFORE the entire collection instance is overwritten
        partial void OnTemplateTargetsChanging(ObservableCollection<TemplateTarget>? oldValue, ObservableCollection<TemplateTarget> newValue) {
            if (oldValue != null) {
                oldValue.CollectionChanged -= OnTemplateTargetsCollectionChanged;

                foreach (var target in oldValue) {
                    target.PropertyChanged -= OnChildPropertyChanged;
                }
            }
        }

        // Toolkit Method: Fires AFTER the entire collection instance is overwritten
        partial void OnTemplateTargetsChanged(ObservableCollection<TemplateTarget>? oldValue, ObservableCollection<TemplateTarget> newValue) {
            if (newValue != null) {
                newValue.CollectionChanged += OnTemplateTargetsCollectionChanged;

                foreach (var target in newValue) {
                    target.PropertyChanged += OnChildPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(IsMonitored));
        }

        // Your Event Handler: Fires when items are added/removed from the CURRENT collection
        private void OnTemplateTargetsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (TemplateTarget newItem in e.NewItems) {
                    newItem.PropertyChanged += OnChildPropertyChanged;
                }
            }

            if (e.OldItems != null) {
                foreach (TemplateTarget oldItem in e.OldItems) {
                    oldItem.PropertyChanged -= OnChildPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(IsMonitored));
        }

        // Item Event Handler: Fires when a child's property (like IsMonitored) changes
        private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(TemplateTarget.IsMonitored)) {
                OnPropertyChanged(nameof(IsMonitored));
            }
        }

        #endregion TemplateTargets
        [JsonIgnore]
        [ObservableProperty]
        private bool _isDirty;

        #endregion properties

        #region constructors
        // Primary Constructor: Enforces valid state
        public PlanOrchestrator(string planName, DirectoryInfo sourceDirectory, Guid? id = null) {
            if (string.IsNullOrWhiteSpace(planName)) throw new ArgumentException("Plan name cannot be empty", nameof(planName));

            PlanName = planName;
            SourceDirectory = sourceDirectory ?? throw new ArgumentNullException(nameof(sourceDirectory));
            Id = id ?? Guid.NewGuid();

            // Assigning through the public property triggers the Toolkit's partial methods!
            TemplateTargets = new ObservableCollection<TemplateTarget>();
        }

        // Reconstitution Constructor
        public PlanOrchestrator(PlanOrchestratorPoco poco)
            : this(poco.PlanName, new DirectoryInfo(poco.SourceDirectory), Guid.Parse(poco.Id)) {

            // Re-trigger the setter just in case, though the primary constructor already did it
            var loadedTargets = new ObservableCollection<TemplateTarget>();
            foreach (var targetPoco in poco.TemplateTargets) {
                loadedTargets.Add(new TemplateTarget(targetPoco));
            }

            // Assigning the loaded list to the property wires up all the events perfectly
            TemplateTargets = loadedTargets;
        }
        #endregion constructors

        public PlanOrchestratorPoco ToPoco() {
            return new PlanOrchestratorPoco {
                // Map the ID explicitly
                Id = this.Id.ToString(),
                PlanName = this.PlanName,
                SourceDirectory = this.SourceDirectory.FullName,
                TemplateTargets = new ObservableCollection<TemplateTargetPoco>(this.TemplateTargets.Select(t => t.ToPoco()))
            };
        }
    }
}