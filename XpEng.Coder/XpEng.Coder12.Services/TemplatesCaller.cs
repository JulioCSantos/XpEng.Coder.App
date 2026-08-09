using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;
using XpEng.Coder80.Infrastructure.Extensions;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder12.Services;

public class TemplatesCaller : ITemplatesCaller {

    private IEngineLogger Logger => DIExtensions.ServiceProvider.GetRequiredService<IEngineLogger>();

    public async Task ProcessFileAsync(string planName, IEnumerable<GenerationTarget> targets, string sourceFilePath, string changeType) {
        try {
            var targetList = targets.ToList();
            if (!targetList.Any()) return;

            Logger.Log($"Processing {Path.GetFileName(sourceFilePath)} [{changeType}] across {targetList.Count} target(s)...");

            foreach (var target in targetList) {
                // Pass the TemplatePath down so the metadata knows exactly who it belongs to
                string metadataFilePath = await GenerateMetadataAsync(
                    planName,
                    sourceFilePath,
                    target.TargetDirectory,
                    target.TemplatePath, // New parameter
                    changeType);

                await ExecuteTemplateAsync(target.TemplatePath, metadataFilePath, target.TargetDirectory, sourceFilePath);
            }
        }
        catch (Exception ex) {
            Logger.Log($"Error processing file {Path.GetFileName(sourceFilePath)}: {ex.Message}");
        }
    }

    // ... (GenerateMetadataAsync and ExecuteTemplateAsync remain exactly the same as they only use strings) ...

    public async Task FullSynchronizationAsync(string planName, IEnumerable<GenerationTarget> targets, string sourceDirectoryPath) {
        Logger.Log($"Starting full synchronization...");

        if (!Directory.Exists(sourceDirectoryPath)) {
            Logger.Log("Source directory does not exist.");
            return;
        }

        var files = Directory.GetFiles(sourceDirectoryPath, "*.cs");
        foreach (var file in files) {
            // FIX: Pass planName as the first argument here!
            await ProcessFileAsync(planName, targets, file, "Created");
        }

        Logger.Log($"Synchronization complete.");
    }

    public async Task<string> GenerateMetadataAsync(string planName, string sourceFilePath, string targetDirectory, string templatePath, string changeType) {
        string sourceCode = File.Exists(sourceFilePath) ? await File.ReadAllTextAsync(sourceFilePath) : "";
        string baseFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        string sourceDirectory = Path.GetDirectoryName(sourceFilePath) ?? string.Empty;

        var classes = new List<object>();

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

        // The enriched JSON payload keeps the absolute paths inside the file
        var metadata = new {
            PlanName = planName ?? "UnknownPlan",
            SourceDirectory = sourceDirectory,
            TargetDirectory = targetDirectory,
            TargetTemplate = templatePath,
            SourceFileName = baseFileName,
            ChangeType = changeType,
            Classes = classes
        };

        string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });

        if (!Directory.Exists(targetDirectory)) {
            Directory.CreateDirectory(targetDirectory);
        }

        // 1. Sanitize PlanName (Fallback to "Plan" if null)
        string safePlanName = string.Join("", (planName ?? "Plan").Split(Path.GetInvalidFileNameChars()));

        // 2. Extract the tail of the Target Directory (up to 3 levels)
        // FIX: Explicitly declare as nullable DirectoryInfo? so .Parent doesn't throw a compiler error
        DirectoryInfo? dirInfo = new DirectoryInfo(targetDirectory);
        var tailSegments = new List<string>();

        for (int i = 0; i < 3 && dirInfo != null; i++) {
            tailSegments.Insert(0, dirInfo.Name);
            dirInfo = dirInfo.Parent;
        }

        // Join the folder names with hyphens (e.g., "Tier09-Models-Entities")
        string directoryTail = targetDirectory.GetDirectoryTail(3);

        // 3. Assemble the readable, unique filename delimited by underscores
        // Format: PlanName_DirectoryTail_SourceFileName_Metadata.json
        string metadataFileName = $"{safePlanName}_{directoryTail}_{baseFileName}_Metadata.json";
        string metadataPath = Path.Combine(targetDirectory, metadataFileName);

        await File.WriteAllTextAsync(metadataPath, json);

        return metadataPath;
    }

    public async Task ExecuteTemplateAsync(string templatePath, string metadataFilePath, string targetDirectory, string sourceFilePath) {
        string baseFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
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
            Logger.Log($"Template execution failed for {baseFileName}: {errors}");
        }
        else {
            Logger.Log($"Template execution finished for {baseFileName}.");
        }

        if (File.Exists(metadataFilePath)) File.Delete(metadataFilePath);
        if (File.Exists(t4LogPath)) File.Delete(t4LogPath);
    }
}
