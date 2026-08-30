using System.Collections.Concurrent;
using Mono.TextTemplating;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder12.Services.T4Pipeline; 
[Register]
public sealed class TemplateCompilerService {
    private sealed class HostAwareTemplateGenerator : TemplateGenerator {
        public void SetTemplateFile(string path) => TemplateFile = path;
    }
    private readonly ConcurrentDictionary<string, Lazy<Task<CompiledTemplate>>> _compiled = new();
    private readonly ConcurrentDictionary<string, HostAwareTemplateGenerator> _generators = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    public async Task PrewarmAsync(CancellationToken ct) {
        const string warmup = "<#@ parameter name=\"X\" type=\"System.String\" #>warm<#=X#>";
        await GetOrCompileAsync("__warmup__", "warmup.tt", warmup, ct).ConfigureAwait(false);
        Process("__warmup__", new Dictionary<string, string> { ["X"] = "up" });
    }
    public async Task<CompiledTemplate> GetOrCompileAsync(string templateHash, string templatePath, string templateContent, CancellationToken ct) {
        var lazy = _compiled.GetOrAdd(templateHash, _ => new Lazy<Task<CompiledTemplate>>(() => CompileAsync(templateHash, templatePath, templateContent, ct)));
        return await lazy.Value.ConfigureAwait(false);
    }

    private async Task<CompiledTemplate> CompileAsync(string hash, string templatePath, string templateContent, CancellationToken ct) {
        var generator = new HostAwareTemplateGenerator();
        _generators[hash] = generator;
        _locks[hash] = new SemaphoreSlim(1, 1);

        // Includes are resolved against IncludePaths, not TemplateFile — without this they fall
        // through to the process working directory (…\bin\Debug\). Adding the template's own
        // folder makes "../Shared/Foo.ttinclude" resolve the way an author would expect.
        string? templateDirectory = Path.GetDirectoryName(templatePath);
        if (!string.IsNullOrWhiteSpace(templateDirectory)) generator.IncludePaths.Add(templateDirectory);

        var compiled = await generator.CompileTemplateAsync(templateContent, ct).ConfigureAwait(false);
        generator.SetTemplateFile(templatePath);

        if (compiled == null || generator.Errors.HasErrors) {
            var errorText = string.Join("; ", generator.Errors.Cast<System.CodeDom.Compiler.CompilerError>().Select(e => e.ErrorText));
            throw new InvalidOperationException($"Template compilation failed for '{templatePath}': {(string.IsNullOrEmpty(errorText) ? "unknown error (compiled result was null)" : errorText)}");
        }
        return compiled;
    }


    public string Process(string templateHash, IDictionary<string, string> sessionParameters) {
        if (!_compiled.TryGetValue(templateHash, out var lazy) || !lazy.IsValueCreated) throw new InvalidOperationException($"Template '{templateHash}' was not compiled before Process was called.");
        var compiled = lazy.Value.Result;
        var generator = _generators[templateHash];
        var gate = _locks[templateHash];
        gate.Wait();
        try {
            var session = generator.GetOrCreateSession();
            session.Clear();
            foreach (var kvp in sessionParameters) session.Add(kvp.Key, kvp.Value);
            return compiled.Process();
        }
        finally { gate.Release(); }
    }
    public void Invalidate(string templateHash) {
        _compiled.TryRemove(templateHash, out _);
        _generators.TryRemove(templateHash, out _);
        if (_locks.TryRemove(templateHash, out var gate)) gate.Dispose();
    }
}