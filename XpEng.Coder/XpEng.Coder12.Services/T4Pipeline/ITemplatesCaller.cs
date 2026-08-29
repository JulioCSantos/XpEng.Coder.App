using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XpEng.Coder12.Services.T4Pipeline {
    // No Tier 09 references here
    public interface ITemplatesCaller {

        // Orchestration Methods
        Task ProcessBatchAsync(string planName, IEnumerable<GenerationTarget> targets, IEnumerable<FileChange> fileChanges);

        Task<bool> ExecuteSetupAsync(string templatePath, string solutionFilePath);

        Task FullSynchronizationAsync(string planName, IEnumerable<GenerationTarget> targets, string sourceDirectoryPath);

        // Individual Execution Methods (Exposed for isolated Unit Testing)
        Task<string> GenerateMetadataAsync(string planName, string targetDirectory, string templatePath, IEnumerable<FileChange> fileChanges);

        Task<bool> ExecuteTemplateAsync(string templatePath, string metadataFilePath, string targetDirectory,
            int fileCount);
    }
}