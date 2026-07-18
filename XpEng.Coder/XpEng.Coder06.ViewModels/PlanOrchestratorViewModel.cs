using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XpEng.Coder09.Models.Entities;

namespace XpEng.Coder06.ViewModels {
    public partial class PlanOrchestratorViewModel : ObservableObject {

        // Expose the raw domain model so the UI can bind directly to its properties
        public PlanOrchestrator Model { get; }

        // UI-specific state (does not belong in the domain!)
        [ObservableProperty]
        private bool _isExpanded = true;

        public PlanOrchestratorViewModel(PlanOrchestrator model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        [RelayCommand]
        private void AddTemplateTarget() {
            // Semper Validus: We must provide valid paths upon instantiation.
            // Note: For now, we use placeholders. In the next iteration, 
            // this should trigger a File/Folder Picker service.
            var dummyTargetDir = new DirectoryInfo(@"C:\NewTargetDirectory");
            var dummyTemplateFile = new FileInfo(@"C:\NewTemplate.tt");

            var newTarget = new TemplateTarget(dummyTargetDir, dummyTemplateFile, isMonitored: true);
            Model.TemplateTargets.Add(newTarget);
        }

        [RelayCommand]
        private void DeleteTemplateTarget(TemplateTarget? target) {
            if (target != null) {
                Model.TemplateTargets.Remove(target);
            }
        }
    }
}