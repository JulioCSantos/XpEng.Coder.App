using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Text.Json;

namespace XpEng.Coder12.Services {
    public class TemplatesCaller : ITemplatesCaller {
        public async Task ProcessFileAsync(string sourceFilePath, string templatePath, string targetDirectory, string changeType, Action<string> logToUi) {
            try {
                logToUi($"Processing {Path.GetFileName(sourceFilePath)} [{changeType}]...");

                string metadataFilePath = await GenerateMetadataAsync(sourceFilePath, targetDirectory, changeType, logToUi);
                await ExecuteTemplateAsync(templatePath, metadataFilePath, targetDirectory, sourceFilePath, logToUi);
            }
            catch (Exception ex) {
                logToUi($"Error processing file {Path.GetFileName(sourceFilePath)}: {ex.Message}");
            }
        }

        public async Task<string> GenerateMetadataAsync(string sourceFilePath, string targetDirectory, string changeType, Action<string> logToUi) {
            string sourceCode = File.Exists(sourceFilePath) ? await File.ReadAllTextAsync(sourceFilePath) : "";
            string baseFileName = Path.GetFileNameWithoutExtension(sourceFilePath);

            var classes = new List<object>();

            // Only parse Roslyn syntax if it is not a deletion
            if (changeType != "Deleted" && !string.IsNullOrWhiteSpace(sourceCode)) {
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = await syntaxTree.GetRootAsync();

                classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Select(c => new {
                    ClassName = c.Identifier.Text,
                    Properties = c.DescendantNodes().OfType<PropertyDeclarationSyntax>().Select(p => new {
                        Name = p.Identifier.Text,
                        Type = p.Type.ToString()
                    }).ToList()
                }).Cast<object>().ToList();
            }

            // The metadata contract handed to the T4 Template
            var metadata = new {
                SourceFileName = baseFileName,
                ChangeType = changeType,
                Classes = classes
            };

            string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });

            if (!Directory.Exists(targetDirectory)) {
                Directory.CreateDirectory(targetDirectory);
            }

            string metadataPath = Path.Combine(targetDirectory, $"{baseFileName}_Metadata.json");
            await File.WriteAllTextAsync(metadataPath, json);

            return metadataPath;
        }

        public async Task ExecuteTemplateAsync(string templatePath, string metadataFilePath, string targetDirectory, string sourceFilePath, Action<string> logToUi) {
            string baseFileName = Path.GetFileNameWithoutExtension(sourceFilePath);

            // Route standard T4 output to a temporary log since the template handles actual file generation internally
            string t4LogPath = Path.Combine(targetDirectory, $"{baseFileName}_Execution.log");

            var processInfo = new ProcessStartInfo {
                FileName = "t4",
                Arguments = $"\"{templatePath}\" -o \"{t4LogPath}\" -p:MetadataFilePath=\"{metadataFilePath}\" -p:TargetDirectory=\"{targetDirectory}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            string errors = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0) {
                logToUi($"Template execution failed for {baseFileName}: {errors}");
            }
            else {
                logToUi($"Template execution finished for {baseFileName}.");
            }

            // Cleanup the metadata file so the target folder stays clean
            if (File.Exists(metadataFilePath)) {
                File.Delete(metadataFilePath);
            }
            // Optional: clean up the execution log as well if we aren't reading manifests yet
            if (File.Exists(t4LogPath)) {
                File.Delete(t4LogPath);
            }
        }

        public async Task FullSynchronizationAsync(string sourceDirectory, string targetDirectory, string templatePath, Action<string> logToUi) {
            logToUi("Starting full synchronization...");

            if (!Directory.Exists(sourceDirectory)) {
                logToUi("Source directory does not exist.");
                return;
            }

            var files = Directory.GetFiles(sourceDirectory, "*.cs");
            foreach (var file in files) {
                // Full sync treats all files as Created/Modified
                await ProcessFileAsync(file, templatePath, targetDirectory, "Created", logToUi);
            }

            logToUi("Synchronization complete.");
        }
    }
}