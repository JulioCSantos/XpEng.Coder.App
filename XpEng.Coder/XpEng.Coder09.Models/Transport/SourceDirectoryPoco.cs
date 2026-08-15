using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models.Validation;
using XpEng.Coder80.Infrastructure.Extensions;

namespace XpEng.Coder09.Models.Transport {

    public partial class SourceDirectoryPoco : ObservableValidator {

        #region Properties
        [ObservableProperty]
        [property: JsonPropertyOrder(1)]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        [property: JsonPropertyOrder(2)]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Source directory cannot be empty.")]
        [PathSyntax]
        [NotifyPropertyChangedFor(nameof(SolutionsDirectory))]
        private string _sourceDirectory = string.Empty;

        [JsonIgnore]
        public string SolutionsDirectory => DirectorySearchExtensions.FindAncestorSolutionRoot(SourceDirectory) ?? SourceDirectory;

        #region TargetTemplates
        [JsonPropertyOrder(3)]
        public ObservableCollection<TargetTemplatePoco> TargetTemplates {
            get => field ??= CreateTrackedCollection();
            set {
                if (field != null) DetachTracking(field);
                field = value;
                AttachTracking(field);
            }
        }

        private ObservableCollection<TargetTemplatePoco> CreateTrackedCollection() {
            var collection = new ObservableCollection<TargetTemplatePoco>();
            AttachTracking(collection);
            return collection;
        }

        private void AttachTracking(ObservableCollection<TargetTemplatePoco> collection) {
            foreach (var poco in collection) AttachTargetTemplate(poco);
            collection.CollectionChanged += OnTargetTemplatesCollectionChanged;
        }

        private void DetachTracking(ObservableCollection<TargetTemplatePoco> collection) {
            collection.CollectionChanged -= OnTargetTemplatesCollectionChanged;
            foreach (var poco in collection) DetachTargetTemplate(poco);
        }

        private void OnTargetTemplatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) foreach (TargetTemplatePoco poco in e.NewItems) AttachTargetTemplate(poco);
            if (e.OldItems != null) foreach (TargetTemplatePoco poco in e.OldItems) DetachTargetTemplate(poco);
            OnPropertyChanged(nameof(HasValidUnmonitoredCards));
            OnPropertyChanged(nameof(HasInvalidPopulatedCards));
        }

        private void AttachTargetTemplate(TargetTemplatePoco poco) {
            poco.Parent = this;
            poco.PropertyChanged += OnTargetTemplatePropertyChanged;
        }

        private void DetachTargetTemplate(TargetTemplatePoco poco) {
            poco.PropertyChanged -= OnTargetTemplatePropertyChanged;
        }

        private void OnTargetTemplatePropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName is nameof(TargetTemplatePoco.IsMonitored) or nameof(TargetTemplatePoco.IsEmpty) or nameof(TargetTemplatePoco.CanBeMonitored)) {
                OnPropertyChanged(nameof(HasValidUnmonitoredCards));
                OnPropertyChanged(nameof(HasInvalidPopulatedCards));
            }
        }
        #endregion TargetTemplates

        [JsonIgnore]
        public bool HasValidUnmonitoredCards => TargetTemplates.Any(t => !t.IsEmpty && t.CanBeMonitored && !t.IsMonitored);

        [JsonIgnore]
        public bool HasInvalidPopulatedCards => TargetTemplates.Any(t => !t.IsEmpty && !t.CanBeMonitored);

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(SourceDirectory) && TargetTemplates.All(t => t.IsEmpty);
        #endregion Properties

        #region Constructors
        public SourceDirectoryPoco() { }
        #endregion Constructors

        #region Event Handlers & Methods
        public bool IsStructurallyValid() {
            if (string.IsNullOrWhiteSpace(SourceDirectory) || HasErrors) {
                return false;
            }

            var populatedTargetTemplates = TargetTemplates.Where(t => !t.IsEmpty).ToList();
            if (populatedTargetTemplates.Any(t => !t.IsStructurallyValid())) {
                return false;
            }

            return true;
        }
        #endregion Event Handlers & Methods
    }
}