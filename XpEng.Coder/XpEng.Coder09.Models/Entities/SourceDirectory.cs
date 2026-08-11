using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models.Transport;
using XpEng.Coder80.Infrastructure.Extensions;

namespace XpEng.Coder09.Models.Entities {
    public partial class SourceDirectory : ObservableObject {

        #region properties
        public Guid Id { get; }

        // Named SourcePath (not SourceDirectory) to avoid the self-referencing
        // SourceDirectory.SourceDirectory echo — same convention as TargetTemplate,
        // which does not have this issue since its class name differs from its properties.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SolutionsDirectory))]
        private DirectoryInfo _sourcePath;

        // Calculated, not persisted: default path for browse dialogs under this Source Directory.
        // Walks upward from SourcePath looking for a .sln/.slnx; falls back to SourcePath itself.
        public string SolutionsDirectory => DirectorySearchExtensions.FindAncestorSolutionRoot(SourcePath?.FullName) ?? SourcePath?.FullName ?? string.Empty;

        #region TargetTemplates
        [ObservableProperty]
        private ObservableCollection<TargetTemplate> _targetTemplates;
        #endregion TargetTemplates
        #endregion properties

        #region constructors
        public SourceDirectory(DirectoryInfo sourcePath, Guid? id = null) {
            SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
            Id = id ?? Guid.NewGuid();
            TargetTemplates = new ObservableCollection<TargetTemplate>();
        }

        public SourceDirectory(SourceDirectoryPoco poco)
            : this(new DirectoryInfo(poco.SourceDirectory), Guid.Parse(poco.Id)) {

            TargetTemplates = new ObservableCollection<TargetTemplate>(poco.TargetTemplates.Select(t => new TargetTemplate(t)));
        }
        #endregion constructors

        public SourceDirectoryPoco ToPoco() {
            return new SourceDirectoryPoco {
                Id = this.Id.ToString(),
                SourceDirectory = this.SourcePath.FullName,
                TargetTemplates = new ObservableCollection<TargetTemplatePoco>(this.TargetTemplates.Select(t => t.ToPoco()))
            };
        }
    }
}