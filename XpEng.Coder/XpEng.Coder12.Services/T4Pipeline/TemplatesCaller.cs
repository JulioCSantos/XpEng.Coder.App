using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XpEng.Coder80.Infrastructure;
using XpEng.Coder80.Infrastructure.Extensions;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder12.Services.T4Pipeline;

[Register(typeof(ITemplatesCaller), ServiceLifetime.Transient)]
public class TemplatesCaller : ITemplatesCaller {

    private IEngineLogger Logger => ApplicationServices.Provider.GetRequiredService<IEngineLogger>();
    private TemplateCallerClient Caller => ApplicationServices.Provider.GetRequiredService<TemplateCallerClient>();

    public async Task ProcessBatchAsync(string planName, IEnumerable<GenerationTarget> targets, IEnumerable<FileChange> fileChanges) {
        try {
            var targetList = targets.ToList();
            var changeList = fileChanges.ToList();
            if (!targetList.Any() || !changeList.Any()) return;

            Logger.Log($"Processing batch of {changeList.Count} file(s) across {targetList.Count} target(s)...");

            foreach (var target in targetList) {
                // ONE metadata file and ONE t4 invocation per target, covering every
                // changed file in the batch — not one invocation per file.
                string metadataFilePath = await GenerateMetadataAsync(planName, target.TargetDirectory, target.TemplatePath, changeList);
                await ExecuteTemplateAsync(target.TemplatePath, metadataFilePath, target.TargetDirectory, changeList.Count);
            }
        }
        catch (Exception ex) {
            Logger.Log($"Error processing batch: {ex.Message}");
        }
    }

    public async Task<bool> ExecuteSetupAsync(string templatePath, string solutionFilePath) {
        string templateName = Path.GetFileNameWithoutExtension(templatePath);

        string templateContent = await File.ReadAllTextAsync(templatePath);
        string templateHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(templateContent)));

        // Every argument the template needs goes into the file — not into session parameters —
        // so the same template can be run manually from inside the solution with no host support.
        var arguments = new Dictionary<string, string> {
            ["SolutionFilePath"] = solutionFilePath
        };

        string argsFilePath;
        try {
            argsFilePath = TemplateArgumentsFile.Write(templatePath, solutionFilePath, arguments);
            Logger.Log($"Arguments written to {argsFilePath}");
        }
        catch (Exception ex) {
            Logger.Log($"Setup failed for {templateName}: could not write the arguments file. {ex.Message}");
            return false;
        }

        // ArgsFilePath is the ONLY session parameter. It exists solely so the template can skip
        // the walk-up search; everything else comes from the file itself.
        var sessionParameters = new Dictionary<string, string> {
            ["ArgsFilePath"] = argsFilePath
        };

        var response = await Caller.GenerateAsync(templatePath, templateContent, templateHash, sessionParameters, string.Empty, CancellationToken.None);

        if (!response.Success) Logger.Log($"Setup failed for {templateName}: {response.ErrorMessage}");
        else Logger.Log($"Setup complete for {templateName} in {response.ElapsedMilliseconds}ms.");

        return response.Success;
    }

    public async Task FullSynchronizationAsync(string planName, IEnumerable<GenerationTarget> targets, string sourceDirectoryPath) {
        Logger.Log($"Starting full synchronization...");

        if (!Directory.Exists(sourceDirectoryPath)) {
            Logger.Log("Source directory does not exist.");
            return;
        }

        var files = Directory.GetFiles(sourceDirectoryPath, "*.cs");
        var fileChanges = files.Select(f => new FileChange(f, "Created")).ToList();
        await ProcessBatchAsync(planName, targets, fileChanges);

        Logger.Log($"Synchronization complete.");
    }

    public async Task<string> GenerateMetadataAsync(string planName, string targetDirectory, string templatePath, IEnumerable<FileChange> fileChanges) {
        var changeList = fileChanges.ToList();
        var fileEntries = new List<object>();

        foreach (var change in changeList) {
            string baseFileName = Path.GetFileNameWithoutExtension(change.SourceFilePath);
            string? oldBaseFileName = string.IsNullOrWhiteSpace(change.OldSourceFilePath) ? null : Path.GetFileNameWithoutExtension(change.OldSourceFilePath);

            var classes = new List<object>();

            if (change.ChangeType != "Deleted" && File.Exists(change.SourceFilePath)) {
                string sourceCode = await File.ReadAllTextAsync(change.SourceFilePath);

                if (!string.IsNullOrWhiteSpace(sourceCode)) {
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
            }

            fileEntries.Add(new {
                SourceFileName = baseFileName,
                SourceDirectory = Path.GetDirectoryName(change.SourceFilePath) ?? string.Empty,
                ChangeType = change.ChangeType,
                OldSourceFileName = oldBaseFileName,
                Classes = classes
            });
        }

        var metadata = new {
            PlanName = planName ?? "UnknownPlan",
            TargetDirectory = targetDirectory,
            TargetTemplate = templatePath,
            Files = fileEntries
        };

        string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });

        if (!Directory.Exists(targetDirectory)) {
            Directory.CreateDirectory(targetDirectory);
        }

        string safePlanName = string.Join("", (planName ?? "Plan").Split(Path.GetInvalidFileNameChars()));
        string directoryTail = targetDirectory.GetDirectoryTail(3);
        string batchTag = Guid.NewGuid().ToString("N").Substring(0, 8);
        string metadataFileName = $"{safePlanName}_{directoryTail}_batch-{batchTag}_Metadata.json";
        string metadataPath = Path.Combine(targetDirectory, metadataFileName);

        await File.WriteAllTextAsync(metadataPath, json);

        return metadataPath;
    }

    public async Task<bool> ExecuteTemplateAsync(string templatePath, string metadataFilePath, string targetDirectory, int fileCount) {
        string templateName = Path.GetFileNameWithoutExtension(templatePath);

        string templateContent = await File.ReadAllTextAsync(templatePath);
        string templateHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(templateContent)));

        var sessionParameters = new Dictionary<string, string> {
            ["MetadataFilePath"] = metadataFilePath,
            ["TargetDirectory"] = targetDirectory
        };

        var response = await Caller.GenerateAsync(templatePath, templateContent, templateHash, sessionParameters, targetDirectory, CancellationToken.None);

        if (!response.Success) {
            Logger.Log($"Template execution failed for {templateName} ({fileCount} file(s)): {response.ErrorMessage}");
        }
        else {
            Logger.Log($"Template execution finished for {templateName} ({fileCount} file(s)) in {response.ElapsedMilliseconds}ms.");
        }

        if (File.Exists(metadataFilePath)) File.Delete(metadataFilePath);
        return response.Success;
    }
}