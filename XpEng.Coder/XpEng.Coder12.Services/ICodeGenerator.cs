using System;
using System.Threading.Tasks;

namespace XpEng.Coder12.Services {
    public interface ICodeGenerator {
        // 1. Extracts the metadata from the source C# file and saves it to SyntaxMetadata.json
        // Returns the path to the generated JSON file.
        Task<string> GenerateMetadataAsync(string sourceFilePath, string targetDirectory, Action<string> logToUi);

        // 2. Executes the T4 template via the local dotnet-t4 tool, passing the JSON path as a parameter.
        Task ExecuteTemplateAsync(string templatePath, string metadataFilePath, string targetDirectory, Action<string> logToUi);

        // 3. The pipeline coordinator: calls GenerateMetadataAsync then ExecuteTemplateAsync.
        // Used primarily by the DirectoryWatcher for single-file changes.
        Task ProcessFileAsync(string sourceFilePath, string templatePath, string targetDirectory, Action<string> logToUi);

        // 4. Batch processor: Loops through all .cs files in the source directory and processes them.
        Task FullSynchronizationAsync(string sourceDirectory, string targetDirectory, string templatePath, Action<string> logToUi);
    }
}