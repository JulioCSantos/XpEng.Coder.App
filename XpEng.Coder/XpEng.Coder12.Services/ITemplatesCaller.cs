using System;
using System.Threading.Tasks;

namespace XpEng.Coder12.Services {
    public interface ITemplatesCaller {
        Task ProcessFileAsync(string sourceFilePath, string templatePath, string targetDirectory, string changeType, Action<string> logToUi);
        Task<string> GenerateMetadataAsync(string sourceFilePath, string targetDirectory, string changeType, Action<string> logToUi);
        Task ExecuteTemplateAsync(string templatePath, string metadataFilePath, string targetDirectory, string sourceFilePath, Action<string> logToUi);
        Task FullSynchronizationAsync(string sourceDirectory, string targetDirectory, string templatePath, Action<string> logToUi);
    }
}