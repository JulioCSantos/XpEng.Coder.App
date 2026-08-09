using System;
using System.Threading.Tasks;

namespace XpEng.Coder12.Services {
        // No Tier 09 references here
    public interface ITemplatesCaller {

        // Orchestration Methods
        Task ProcessFileAsync(string planName, IEnumerable<GenerationTarget> targets, string sourceFilePath, string changeType);

        Task FullSynchronizationAsync(string planName, IEnumerable<GenerationTarget> targets, string sourceDirectoryPath);

        // Individual Execution Methods (Exposed for isolated Unit Testing)
        Task<string> GenerateMetadataAsync(string planName, string sourceFilePath, string targetDirectory, string templatePath, string changeType);

        Task ExecuteTemplateAsync(string templatePath, string metadataFilePath, string targetDirectory, string sourceFilePath);
    }
}