using System;

namespace XpEng.Coder12.Services {

    // Pure Tier 12 data structure. Knows nothing about Models.
    public class GenerationTarget(string templatePath, string targetDirectory) {
        public string TemplatePath { get; } = templatePath;
        public string TargetDirectory { get; } = targetDirectory;
    }
}