using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

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
        [JsonPropertyOrder(4)] // Regular property syntax
        public ObservableCollection<SourceDirectoryPoco> SourceDirectories {
            get => field ??= new ObservableCollection<SourceDirectoryPoco>();
            set => field = value;
        }
        #endregion SourceDirectories

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