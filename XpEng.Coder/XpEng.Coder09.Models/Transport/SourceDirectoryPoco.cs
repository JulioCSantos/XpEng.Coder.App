using System;
using System.Collections.ObjectModel;
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

        // Calculated, not persisted: default path for browse dialogs under this Source Directory.
        // Walks upward from SourceDirectory looking for a .sln/.slnx; falls back to SourceDirectory itself.
        [JsonIgnore]
        public string SolutionsDirectory => DirectorySearchExtensions.FindAncestorSolutionRoot(SourceDirectory) ?? SourceDirectory;

        #region TargetTemplates
        [JsonPropertyOrder(3)] // Regular property syntax
        public ObservableCollection<TargetTemplatePoco> TargetTemplates {
            get => field ??= new ObservableCollection<TargetTemplatePoco>();
            set => field = value;
        }
        #endregion TargetTemplates

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