using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder09.Models.Entities {
    public partial class PlanOrchestrator : ObservableObject {

        #region properties
        public Guid Id { get; }

        [ObservableProperty]
        private string _planName;

        [ObservableProperty]
        private bool _isMonitored = true;

        #region SourceDirectories
        [ObservableProperty]
        private ObservableCollection<SourceDirectory> _sourceDirectories;
        #endregion SourceDirectories

        [JsonIgnore]
        [ObservableProperty]
        private bool _isDirty;

        #endregion properties

        #region constructors
        // Primary Constructor: Enforces valid state
        public PlanOrchestrator(string planName, Guid? id = null) {
            if (string.IsNullOrWhiteSpace(planName)) throw new ArgumentException("Plan name cannot be empty", nameof(planName));

            PlanName = planName;
            Id = id ?? Guid.NewGuid();
            SourceDirectories = new ObservableCollection<SourceDirectory>();
        }

        // Reconstitution Constructor
        public PlanOrchestrator(PlanOrchestratorPoco poco)
            : this(poco.PlanName, Guid.Parse(poco.Id)) {

            IsMonitored = poco.IsMonitored;
            SourceDirectories = new ObservableCollection<SourceDirectory>(poco.SourceDirectories.Select(s => new SourceDirectory(s)));
        }
        #endregion constructors

        public PlanOrchestratorPoco ToPoco() {
            return new PlanOrchestratorPoco {
                Id = this.Id.ToString(),
                PlanName = this.PlanName,
                IsMonitored = this.IsMonitored,
                SourceDirectories = new ObservableCollection<SourceDirectoryPoco>(this.SourceDirectories.Select(s => s.ToPoco()))
            };
        }
    }
}