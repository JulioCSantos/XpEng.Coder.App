using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models.Validation;
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
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Source directory cannot be empty.")]
        [PathSyntax]
        [NotifyPropertyChangedFor(nameof(SourceDirectoryDisplay))]
        private string _sourceDirectory = string.Empty;

        [JsonIgnore]
        public string SourceDirectoryDisplay => StringExtensions.Shorten(SourceDirectory);

        [ObservableProperty]
        [property: JsonIgnore]
        private bool _isEditingSourceDirectory;

        #region TemplateTargets
        [JsonPropertyOrder(4)] // Regular property syntax
        public ObservableCollection<TemplateTargetPoco> TemplateTargets {
            get => field ??= new ObservableCollection<TemplateTargetPoco>();
            set => field = value;
        }
        #endregion TemplateTargets

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(PlanName) && string.IsNullOrWhiteSpace(SourceDirectory);
        #endregion Properties

        #region Constructors
        public PlanOrchestratorPoco() { }
        #endregion Constructors

        #region Event Handlers & Methods
        public bool IsStructurallyValid() {
            if (string.IsNullOrWhiteSpace(PlanName) || string.IsNullOrWhiteSpace(SourceDirectory) || HasErrors) {
                return false;
            }

            var populatedTargets = TemplateTargets.Where(t => !t.IsEmpty).ToList();
            if (populatedTargets.Any(t => !t.IsStructurallyValid())) {
                return false;
            }

            return true;
        }
        #endregion Event Handlers & Methods
    }
}