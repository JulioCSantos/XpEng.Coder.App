using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models.Validation;

namespace XpEng.Coder09.Models.Transport {

    public partial class PlanOrchestratorPoco : ObservableValidator {

        #region Properties
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Plan name cannot be empty.")]
        private string _planName = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Source directory cannot be empty.")]
        [PathSyntax]
        private string _sourceDirectory = string.Empty;

        #region TemplateTargets
        public ObservableCollection<TemplateTargetPoco> TemplateTargets {
            get {
                if (field == null) {
                    field = new ObservableCollection<TemplateTargetPoco>();
                }
                return field;
            }
            set {
                field = value;
            }
        }
        #endregion TemplateTargets

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(PlanName) && string.IsNullOrWhiteSpace(SourceDirectory);
        #endregion Properties

        #region Constructors
        public PlanOrchestratorPoco() { }
        #endregion Constructors

        #region Event Handlers & Methods
        // Silently checks valid state without forcing UI validation errors
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