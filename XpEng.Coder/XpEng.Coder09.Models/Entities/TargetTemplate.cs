using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder09.Models.Entities {
    public partial class TargetTemplate : ObservableObject {

        #region properties
        public Guid Id { get; }

        [ObservableProperty]
        private DirectoryInfo _targetDirectory;

        [ObservableProperty]
        private FileInfo _templatePath;

        [ObservableProperty]
        private bool _isMonitored;
        #endregion properties

        #region constructors
        public TargetTemplate(DirectoryInfo targetDirectory, FileInfo templatePath, bool isMonitored, Guid? id = null) {
            TargetDirectory = targetDirectory ?? throw new ArgumentNullException(nameof(targetDirectory));
            TemplatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
            IsMonitored = isMonitored;
            Id = id ?? Guid.NewGuid();
        }

        public TargetTemplate(TargetTemplatePoco poco)
            : this(new DirectoryInfo(poco.TargetDirectory), new FileInfo(poco.TemplatePath), poco.IsMonitored, Guid.Parse(poco.Id)) {
        }
        #endregion constructors

        public TargetTemplatePoco ToPoco() {
            return new TargetTemplatePoco {
                Id = this.Id.ToString(),
                TargetDirectory = this.TargetDirectory.FullName,
                TemplatePath = this.TemplatePath.FullName,
                IsMonitored = this.IsMonitored
            };
        }
    }
}