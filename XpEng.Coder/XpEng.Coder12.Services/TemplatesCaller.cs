using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XpEng.Coder12.Services {
    public class TemplatesCaller : ITemplatesCaller {
        public async Task ProcessFileAsync(string sourceFilePath, string templatePath, string targetDirectory, Action<string> logToUi) {
            try {
                logToUi($"Processing {Path.GetFileName(sourceFilePath)}...");

                // 1. Generate the JSON Metadata
                string metadataFilePath = await GenerateMetadataAsync(sourceFilePath, targetDirectory, logToUi);

                // 2. Execute the Template
                await ExecuteTemplateAsync(templatePath, metadataFilePath, targetDirectory, logToUi);
            }
            catch (Exception ex) {
                logToUi($"Error processing file: {ex.Message}");
            }
        }

        public async Task<string> GenerateMetadataAsync(string sourceFilePath, string targetDirectory, Action<string> logToUi) {
            string sourceCode = await File.ReadAllTextAsync(sourceFilePath);

            // Parse the C# file using Roslyn
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = await syntaxTree.GetRootAsync();

            // Extract the first class name
            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            string className = classDeclaration?.Identifier.Text ?? Path.GetFileNameWithoutExtension(sourceFilePath);

            // Extract the properties
            // Extract the properties and cast them to object to keep the compiler happy
            var properties = classDeclaration?
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => (object)new {
                    Name = p.Identifier.Text,
                    Type = p.Type.ToString()
                }).ToList() ?? new List<object>();

            // Build the dynamic DTO
            var metadata = new {
                ClassName = className,
                Properties = properties
                // TODO: Expand with Methods, Enums, etc., as needed
            };

            // Serialize to JSON
            string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });

            // Ensure target directory exists
            if (!Directory.Exists(targetDirectory)) {
                Directory.CreateDirectory(targetDirectory);
            }

            string metadataPath = Path.Combine(targetDirectory, "SyntaxMetadata.json");
            await File.WriteAllTextAsync(metadataPath, json);

            logToUi($"Metadata generated: SyntaxMetadata.json");
            return metadataPath;
        }

        public async Task ExecuteTemplateAsync(string templatePath, string metadataFilePath, string targetDirectory, Action<string> logToUi) {
            // Read the ClassName back from the JSON to name our output file correctly
            string json = await File.ReadAllTextAsync(metadataFilePath);
            using var doc = JsonDocument.Parse(json);
            string className = doc.RootElement.TryGetProperty("ClassName", out var nameProp)
                ? nameProp.GetString() ?? "Generated"
                : "Generated";

            string outputFilePath = Path.Combine(targetDirectory, $"{className}ViewModel.cs");

            // We use the dotnet-t4 CLI tool to execute the template asynchronously
            // This avoids complex AppDomain or NuGet dependency lock-ins
            var processInfo = new ProcessStartInfo {
                FileName = "t4",
                Arguments = $"\"{templatePath}\" -o \"{outputFilePath}\" -p:MetadataFilePath=\"{metadataFilePath}\" -p:TargetDirectory=\"{targetDirectory}\"",
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
                logToUi($"Template execution failed: {errors}");
            }
            else {
                logToUi($"Generated output: {Path.GetFileName(outputFilePath)}");
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
                await ProcessFileAsync(file, templatePath, targetDirectory, logToUi);
            }

            logToUi("Synchronization complete.");
        }
    }
}