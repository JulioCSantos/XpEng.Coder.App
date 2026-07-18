using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models.Validation;

namespace XpEng.Coder09.Models.Transport {

    public partial class TemplateTargetPoco : ObservableValidator {

        #region Properties
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Target directory cannot be empty.")]
        [PathSyntax]
        private string _targetDirectory = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Template path cannot be empty.")]
        [PathSyntax]
        private string _templatePath = string.Empty;

        [ObservableProperty]
        private bool _isMonitored = true;

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(TargetDirectory) && string.IsNullOrWhiteSpace(TemplatePath);
        #endregion Properties

        #region Constructors
        public TemplateTargetPoco() { }
        #endregion Constructors

        #region Event Handlers & Methods
        // Silently checks valid state without forcing UI validation errors
        public bool IsStructurallyValid() {
            return !string.IsNullOrWhiteSpace(TargetDirectory) &&
                   !string.IsNullOrWhiteSpace(TemplatePath) &&
                   !HasErrors;
        }
        #endregion Event Handlers & Methods
    }
}