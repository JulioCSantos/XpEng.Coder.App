using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder80.Infrastructure.Extensions;

namespace XpEng.Coder09.Models.Transport {

    public partial class PlanOrchestratorPoco : ObservableValidator {

        #region Properties
        [ObservableProperty]
        [property: JsonPropertyOrder(1)]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        [property: JsonPropertyOrder(2)]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Plan name cannot be empty.")]
        private string _planName = string.Empty;

        [ObservableProperty]
        [property: JsonPropertyOrder(3)]
        private bool _isMonitored = true;

        #region SourceDirectories
        [JsonPropertyOrder(4)]
        public ObservableCollection<SourceDirectoryPoco> SourceDirectories {
            get => field ??= CreateTrackedCollection();
            set {
                if (field != null) DetachTracking(field);
                field = value;
                AttachTracking(field);
            }
        }

        private ObservableCollection<SourceDirectoryPoco> CreateTrackedCollection() {
            var collection = new ObservableCollection<SourceDirectoryPoco>();
            AttachTracking(collection);
            return collection;
        }

        private void AttachTracking(ObservableCollection<SourceDirectoryPoco> collection) {
            foreach (var source in collection) AttachSourceDirectory(source);
            collection.CollectionChanged += OnSourceDirectoriesCollectionChanged;
        }

        private void DetachTracking(ObservableCollection<SourceDirectoryPoco> collection) {
            collection.CollectionChanged -= OnSourceDirectoriesCollectionChanged;
            foreach (var source in collection) DetachSourceDirectory(source);
        }

        private void OnSourceDirectoriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) foreach (SourceDirectoryPoco source in e.NewItems) AttachSourceDirectory(source);
            if (e.OldItems != null) foreach (SourceDirectoryPoco source in e.OldItems) DetachSourceDirectory(source);
            OnPropertyChanged(nameof(HasValidUnmonitoredCards));
            OnPropertyChanged(nameof(HasInvalidPopulatedCards));
        }

        private void AttachSourceDirectory(SourceDirectoryPoco source) {
            source.PropertyChanged += OnSourceDirectoryPropertyChanged;
        }

        private void DetachSourceDirectory(SourceDirectoryPoco source) {
            source.PropertyChanged -= OnSourceDirectoryPropertyChanged;
        }

        private void OnSourceDirectoryPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName is nameof(SourceDirectoryPoco.HasValidUnmonitoredCards) or nameof(SourceDirectoryPoco.HasInvalidPopulatedCards)) {
                OnPropertyChanged(nameof(HasValidUnmonitoredCards));
                OnPropertyChanged(nameof(HasInvalidPopulatedCards));
            }
        }
        #endregion SourceDirectories

        #region Setup
        // Plan-scoped, one-shot: not part of the SourceDirectories/TargetTemplates watching
        // hierarchy at all. IsSetupActive is expected to be flipped back to false by the
        // orchestration layer once Setup completes successfully — see ForceSync().
        [ObservableProperty]
        [property: JsonPropertyOrder(5)]
        private bool _isSetupActive;

        [ObservableProperty]
        [property: JsonPropertyOrder(6)]
        private string _setupSolutionFile = string.Empty;

        [ObservableProperty]
        [property: JsonPropertyOrder(7)]
        [NotifyPropertyChangedFor(nameof(SetupTemplatePathDisplay))]
        private string _setupTemplatePath = string.Empty;

        [JsonIgnore]
        public string SetupTemplatePathDisplay => StringExtensions.Shorten(SetupTemplatePath);
        #endregion Setup

        [JsonIgnore]
        public bool HasValidUnmonitoredCards => SourceDirectories.Any(s => s.HasValidUnmonitoredCards);

        [JsonIgnore]
        public bool HasInvalidPopulatedCards => SourceDirectories.Any(s => s.HasInvalidPopulatedCards);

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(PlanName) && SourceDirectories.All(s => s.IsEmpty);
        #endregion Properties

        #region Constructors
        public PlanOrchestratorPoco() { }
        #endregion Constructors

        #region Event Handlers & Methods
        public bool IsStructurallyValid() {
            if (string.IsNullOrWhiteSpace(PlanName) || HasErrors) {
                return false;
            }

            var populatedSources = SourceDirectories.Where(s => !s.IsEmpty).ToList();
            if (populatedSources.Any(s => !s.IsStructurallyValid())) {
                return false;
            }

            return true;
        }
        #endregion Event Handlers & Methods
    }
}