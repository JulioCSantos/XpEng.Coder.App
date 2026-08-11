using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models.Validation;
using XpEng.Coder80.Infrastructure.Extensions;

namespace XpEng.Coder09.Models.Transport {

    public partial class TemplateTargetPoco : ObservableValidator {

        #region Properties
        [ObservableProperty]
        [property: JsonPropertyOrder(1)]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        [property: JsonPropertyOrder(2)]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Target directory cannot be empty.")]
        [PathSyntax]
        [NotifyPropertyChangedFor(nameof(TargetDirectoryDisplay))]
        private string _targetDirectory = string.Empty;

        [JsonIgnore]
        public string TargetDirectoryDisplay => StringExtensions.Shorten(TargetDirectory);

        [ObservableProperty]
        [property: JsonIgnore]
        private bool _isEditingTargetDirectory;

        [ObservableProperty]
        [property: JsonPropertyOrder(3)]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Template path cannot be empty.")]
        [PathSyntax]
        [NotifyPropertyChangedFor(nameof(TemplatePathDisplay))]
        private string _templatePath = string.Empty;

        [JsonIgnore]
        public string TemplatePathDisplay => StringExtensions.Shorten(TemplatePath);

        [ObservableProperty]
        [property: JsonIgnore]
        private bool _isEditingTemplatePath;

        [ObservableProperty]
        [property: JsonPropertyOrder(4)]
        private bool _isMonitored = false;

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(TargetDirectory) && string.IsNullOrWhiteSpace(TemplatePath);
        #endregion Properties

        #region Constructors
        public TemplateTargetPoco() { }
        #endregion Constructors

        #region Event Handlers & Methods
        public bool IsStructurallyValid() {
            return !string.IsNullOrWhiteSpace(TargetDirectory) &&
                   !string.IsNullOrWhiteSpace(TemplatePath) &&
                   !HasErrors;
        }
        #endregion Event Handlers & Methods
    }
}