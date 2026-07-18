using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder09.Models.Entities {
    public partial class TemplateTarget : ObservableObject {
        public Guid Id { get; }

        [ObservableProperty]
        private DirectoryInfo _targetDirectory;

        [ObservableProperty]
        private FileInfo _templatePath;

        [ObservableProperty]
        private bool _isMonitored;

        // Primary Constructor: Enforces valid state on creation
        public TemplateTarget(DirectoryInfo targetDirectory, FileInfo templatePath, bool isMonitored = true, Guid? id = null) {
            TargetDirectory = targetDirectory ?? throw new ArgumentNullException(nameof(targetDirectory));
            TemplatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
            IsMonitored = isMonitored;
            Id = id ?? Guid.NewGuid();
        }

        // Reconstitution Constructor: Builds the valid domain object from the transport POCO
        public TemplateTarget(TemplateTargetPoco poco) {
            Id = Guid.Parse(poco.Id);
            TargetDirectory = new DirectoryInfo(poco.TargetDirectory);
            TemplatePath = new FileInfo(poco.TemplatePath);
            IsMonitored = poco.IsMonitored;
        }

        // Export to Transport layer
        public TemplateTargetPoco ToPoco() => new TemplateTargetPoco {
            Id = Id.ToString(),
            TargetDirectory = TargetDirectory.FullName,
            TemplatePath = TemplatePath.FullName,
            IsMonitored = IsMonitored
        };
    }
}
