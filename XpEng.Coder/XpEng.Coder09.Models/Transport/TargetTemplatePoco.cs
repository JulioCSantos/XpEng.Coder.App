using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models.Validation;
using XpEng.Coder80.Infrastructure.Extensions;

namespace XpEng.Coder09.Models.Transport {

    public partial class TargetTemplatePoco : ObservableValidator {

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
        [NotifyPropertyChangedFor(nameof(CanBeMonitored))]
        [NotifyPropertyChangedFor(nameof(IsMonitored))]
        [NotifyPropertyChangedFor(nameof(MonitoringDisabledReason))]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        private string _targetDirectory = string.Empty;

        [JsonIgnore]
        public string TargetDirectoryDisplay => StringExtensions.Shorten(TargetDirectory);

        [ObservableProperty]
        [property: JsonPropertyOrder(3)]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Template path cannot be empty.")]
        [PathSyntax]
        [NotifyPropertyChangedFor(nameof(TemplatePathDisplay))]
        [NotifyPropertyChangedFor(nameof(Name))]
        [NotifyPropertyChangedFor(nameof(CanBeMonitored))]
        [NotifyPropertyChangedFor(nameof(IsMonitored))]
        [NotifyPropertyChangedFor(nameof(MonitoringDisabledReason))]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        private string _templatePath = string.Empty;

        [JsonIgnore]
        public string TemplatePathDisplay => StringExtensions.Shorten(TemplatePath);

        [JsonIgnore]
        public string Name => Path.GetFileName(TemplatePath);

        [JsonIgnore]
        public bool TargetDirectoryExists => !string.IsNullOrWhiteSpace(TargetDirectory) && Directory.Exists(TargetDirectory);

        [JsonIgnore]
        public bool TemplatePathExists => !string.IsNullOrWhiteSpace(TemplatePath) && File.Exists(TemplatePath);

        [JsonIgnore]
        public bool CanBeMonitored => TargetDirectoryExists && TemplatePathExists && !HasErrors;

        [JsonIgnore]
        public string MonitoringDisabledReason {
            get {
                if (CanBeMonitored) return string.Empty;
                var reasons = new List<string>();
                if (string.IsNullOrWhiteSpace(TargetDirectory)) reasons.Add("Target directory is empty");
                else if (!Directory.Exists(TargetDirectory)) reasons.Add("Target directory does not exist");
                if (string.IsNullOrWhiteSpace(TemplatePath)) reasons.Add("Template file is empty");
                else if (!File.Exists(TemplatePath)) reasons.Add("Template file does not exist");
                if (HasErrors) reasons.Add("Fix validation errors above");
                return string.Join(" · ", reasons);
            }
        }

        [ObservableProperty]
        [property: JsonPropertyOrder(4)]
        [property: JsonPropertyName("IsMonitored")]
        [NotifyPropertyChangedFor(nameof(IsMonitored))]
        private bool? _monitoredOverride;

        [JsonIgnore]
        public bool IsMonitored {
            get => CanBeMonitored && (MonitoredOverride ?? (!string.IsNullOrWhiteSpace(TargetDirectory) && !string.IsNullOrWhiteSpace(TemplatePath)));
            set {
                if (MonitoredOverride == value) return;
                MonitoredOverride = value;
            }
        }

        [JsonIgnore]
        public SourceDirectoryPoco? Parent { get; internal set; }

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(TargetDirectory) && string.IsNullOrWhiteSpace(TemplatePath);
        #endregion Properties

        #region Constructors
        public TargetTemplatePoco() {
            PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(HasErrors)) {
                    OnPropertyChanged(nameof(CanBeMonitored));
                    OnPropertyChanged(nameof(IsMonitored));
                    OnPropertyChanged(nameof(MonitoringDisabledReason));
                }
            };
        }
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