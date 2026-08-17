# T4 Template Contract for XpEng.Coder.App

This document defines what a T4 template must do to be invoked correctly by
`XpEng.Coder.App`. Templates live in an arbitrary repository folder — not
necessarily inside any solution Coder.App watches — and are selected by an
operator at runtime, so this contract is the only thing tying a template to
the host correctly. `BasicClassTemplate.tt` and `MainModelGraph.tt` are the
reference implementations; when in doubt, read one of them alongside this
doc.

## Required directives

```csharp
<#@ template language="C#" hostspecific="true" #>
<#@ parameter name="MetadataFilePath" type="System.String" #>
<#@ parameter name="TargetDirectory" type="System.String" #>
```

- `hostspecific="true"` is required — without it, `Host.TemplateFile` and
  other host-specific members aren't available to your template.
- These two parameters are the entire session contract today. Coder.App's
  session values are always `string` — there is no mechanism to pass a
  richer type. If your template needs structured data beyond a file path,
  put it in the metadata JSON itself (see below), not in a new parameter.

## What Coder.App gives you

- **`MetadataFilePath`** — path to a JSON file describing every file change
  in this batch (schema below). Coder.App deletes this file once your
  template invocation returns, so read it during execution — don't defer
  reading it or cache the path for later.
- **`TargetDirectory`** — absolute path your generated output should live
  under. Coder.App has no opinion on filenames or subfolder structure
  within it.

## Metadata JSON schema

```json
{
  "PlanName": "string",
  "TargetDirectory": "string",
  "TargetTemplate": "string — full path to this .tt file",
  "Files": [
    {
      "SourceFileName": "string — no extension",
      "SourceDirectory": "string",
      "ChangeType": "Created | Changed | Deleted | Renamed",
      "OldSourceFileName": "string or null — only meaningful when ChangeType is Renamed",
      "Classes": [
        {
          "ClassName": "string",
          "Properties": [
            { "Name": "string", "Type": "string" }
          ]
        }
      ]
    }
  ]
}
```

`Classes` reflects a Roslyn parse of the source file at the moment the batch
was processed. It can legitimately be an empty array — an empty class, a
non-C# file, or a file that no longer exists all produce `Classes: []`.
Never assume a non-`Deleted` `ChangeType` implies meaningful content. If
your template needs relationship information (base types, navigation
properties, attributes) beyond `Classes`, re-parse the raw source file
yourself via Roslyn using `SourceDirectory`/`SourceFileName` — the metadata
contract intentionally stays thin, and richer analysis is the template's
own responsibility, not something Coder.App computes for you.

## `ChangeType` reflects ground truth, not the raw OS event

Editors — Visual Studio in particular — create and save files through
multi-step temp-file swaps that fire several raw filesystem events for what
is, semantically, one action. Coder.App resolves all of that down to the
file's actual, current state before your template ever sees it:

- **`Deleted`** — the source file does not exist on disk right now,
  regardless of what raw events led here. `Classes` will be empty. Delete
  any output you previously generated for it and do nothing else.
- **`Created`** — this file is new: either genuinely new, or it reappeared
  under this name within the same batch (e.g. an editor's save-via-rename
  pattern). Treat it as having no prior output to clean up.
- **`Changed`** — a previously-known file was modified in place.
- **`Renamed`** — `OldSourceFileName` carries the previous name. Delete the
  output associated with the old name, then generate output for the new
  one.

## One invocation per batch — not per file

Coder.App calls your template **once per batch per active target**, never
once per changed file. Your template must loop over `Files[]` itself. This
is central to why the pipeline is fast: compiling your template is the
expensive part, and Coder.App caches the compiled result by the `.tt`
file's own content hash — it only recompiles when you edit the template,
never per file or per batch. A template written as if it only ever handles
one entry will silently process just the first (or last) file in a batch
and ignore the rest.

## Your template writes its own output

Coder.App does not write anything to disk on your behalf. Whatever text
your template emits via `Write()` or `<#= #>` is captured and returned to
Coder.App purely as an informational string — it is not persisted anywhere.
If you want generated code saved, call `File.WriteAllText` (or equivalent)
yourself, once per file, inside your loop over `Files[]`.

## File filters

Every template should declare its own filter rules at the very top of the
generation block — the one place a maintainer needs to read or edit to
change what the template acts on. Rules are OR'd together; within one rule,
every set criterion must match (AND):

```csharp
var Filters = new[] {
    new FileFilterRule {
        NamePatterns = new[] { "*.cs" },
        ExcludeNamePatterns = new[] { "*DbContext.cs", "*.Designer.cs", "*.g.cs" }
    }
};
```

`FileFilterRule` supports name patterns (wildcard, `*`/`?`), exclude
patterns (checked after name patterns, any match rejects), `ChangeTypes`,
and optional required/excluded base-type checks (only evaluated if set,
since determining base types needs a Roslyn parse — the more expensive
path). See `BasicClassTemplate.tt` or `MainModelGraph.tt` for the full
`FileFilterRule`/`ShouldProcess` helper implementation to copy into a new
template.

Always exclude your own template's generated output pattern (e.g.
`*.g.cs`) from what it treats as source input — otherwise a template can
end up reprocessing its own generated files as if they were hand-written
entities.

## Computed namespace

Templates that generate namespaced C# should compute the namespace from
`TargetDirectory` rather than hardcoding one or adding a new parameter:

1. Walk upward from `TargetDirectory` until a `.csproj` is found.
2. Read its `RootNamespace` element if present; otherwise fall back to the
   `.csproj` filename (the same default the SDK itself uses).
3. Append the path segments between the project root and `TargetDirectory`,
   sanitized for characters that aren't valid in a C# identifier.
4. **Exception:** if `TargetDirectory`'s own leaf folder is literally named
   `AutoGenerated` or `Generated` (case-insensitive), strip that segment
   before joining. This lets generated code share a namespace with
   hand-written code sitting one level up, while still living in its own,
   physically segregated folder — required for partial classes, which must
   match namespace exactly but don't need to share a directory.

See `ComputeNamespace`/`GetRelativePathManual` in either reference template
for the full implementation (`Path.GetRelativePath` is unavailable in this
compile context — use the `Uri`-based manual implementation instead, not
the BCL method).

## Output file naming

Generated files use a `.g.cs` suffix on the **file name only** — e.g.
`Drug.g.cs`, `DrugViewModel.g.cs`. This is a naming convention for
readability and tooling, not a mechanism for avoiding type collisions —
C# type identity is `namespace + type name`, and the file's name plays no
part in that. Collision avoidance comes from the combination of a
segregated output folder and the computed-namespace rule above; if a
genuine collision does happen, the expectation is that the generated file
is simply overwritten with the new content, not deduplicated or renamed.

## Constraints

- **Session values are strings only.** No object graphs, no custom types.
- **Assembly references must resolve outside any solution context, and
  the reference set is more restricted than a normal project build.**
  Coder.App runs your template via a hosted engine, not MSBuild — a
  `<#@ assembly name="..." #>` reference needs to be in the GAC, sitting
  alongside Coder.App's own binaries, or given as an absolute path. In
  practice, several BCL types that "just work" in a normal .csproj need an
  explicit `<#@ assembly #>` line here that you would not otherwise expect:
  `System.Xml`, `System.Xml.Linq`, and `System.Memory` (needed transitively
  by `Regex`'s modern API surface) have all been required by templates
  built so far. Start from the reference block in `MainModelGraph.tt` if
  your template does anything beyond basic string/JSON work, and expect to
  add more if you hit `CS0012`/`CS0234` compile errors — `Path` also lacks
  some newer members (`GetRelativePath`) in this profile; prefer manual
  `Uri`-based implementations over assuming a BCL method is available.
- **Don't rely on state persisting between invocations.** The compiled
  template is reused across many batches over the app's lifetime; treat
  each invocation as a fresh, independent run and avoid depending on
  static or instance state left over from a previous batch.
- **Extension/format filtering is your responsibility.** Coder.App forwards
  every file change in a watched directory regardless of extension or
  content — if your template only cares about certain kinds of source
  files, use the file filters above.
- **Output naming and layout are entirely up to you** beyond the `.g.cs`
  suffix convention above.

## Minimal skeleton

```csharp
<#@ template language="C#" hostspecific="true" #>
<#@ assembly name="System.Text.Json" #>
<#@ import namespace="System.Linq" #>
<#@ import namespace="System.Text.Json" #>
<#@ import namespace="System.Collections.Generic" #>
<#@ import namespace="System.IO" #>
<#@ import namespace="System.Text.RegularExpressions" #>
<#@ parameter name="MetadataFilePath" type="System.String" #>
<#@ parameter name="TargetDirectory" type="System.String" #>
<#
    var Filters = new[] {
        new FileFilterRule { NamePatterns = new[] { "*.cs" }, ExcludeNamePatterns = new[] { "*.g.cs" } }
    };

    if (string.IsNullOrWhiteSpace(MetadataFilePath) || !File.Exists(MetadataFilePath)) { Write("Error: MetadataFilePath is missing or invalid."); return string.Empty; }
    using var document = JsonDocument.Parse(File.ReadAllText(MetadataFilePath));
    var files = document.RootElement.GetProperty("Files");
    int count = 0;
    foreach (var fileEntry in files.EnumerateArray())
    {
        string sourceFileName = fileEntry.GetProperty("SourceFileName").GetString();
        string changeType = fileEntry.GetProperty("ChangeType").GetString();
        if (!ShouldProcess(Filters, $"{sourceFileName}.cs", changeType, () => new List<string>())) continue;

        string outputPath = Path.Combine(TargetDirectory, $"{sourceFileName}.g.cs");
        if (changeType == "Deleted") { if (File.Exists(outputPath)) File.Delete(outputPath); continue; }

        // ...generate your content here, using fileEntry.GetProperty("Classes") as needed...
        File.WriteAllText(outputPath, "// generated content");
        count++;
    }
    Write($"Batch complete: {count} file(s) generated.");
#>
<#+
    private sealed class FileFilterRule {
        public string[] NamePatterns;
        public string[] ExcludeNamePatterns;
        public string[] ChangeTypes;
        public string RequiredBaseType;
        public string[] ExcludedBaseTypes;
    }
    private bool NameMatches(string fileName, string[] patterns) {
        if (patterns == null || patterns.Length == 0) return true;
        return patterns.Any(p => Regex.IsMatch(fileName, "^" + Regex.Escape(p).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase));
    }
    private bool RuleMatches(FileFilterRule rule, string fileName, string changeType, Func<List<string>> getBaseTypes) {
        if (!NameMatches(fileName, rule.NamePatterns)) return false;
        if (rule.ExcludeNamePatterns != null && NameMatches(fileName, rule.ExcludeNamePatterns)) return false;
        if (rule.ChangeTypes != null && rule.ChangeTypes.Length > 0 && !rule.ChangeTypes.Contains(changeType, StringComparer.OrdinalIgnoreCase)) return false;
        if (rule.RequiredBaseType != null || rule.ExcludedBaseTypes != null) {
            var baseTypes = getBaseTypes();
            if (rule.RequiredBaseType != null && !baseTypes.Contains(rule.RequiredBaseType)) return false;
            if (rule.ExcludedBaseTypes != null && rule.ExcludedBaseTypes.Any(baseTypes.Contains)) return false;
        }
        return true;
    }
    private bool ShouldProcess(FileFilterRule[] rules, string fileName, string changeType, Func<List<string>> getBaseTypes)
        => rules.Any(r => RuleMatches(r, fileName, changeType, getBaseTypes));
#>
```

## Reference implementations

- **`BasicClassTemplate.tt`** — one generated ViewModel per changed source
  file, including deletion and rename handling.
- **`MainModelGraph.tt`** — a more advanced example: full scalar-property
  mirroring, foreign-key/navigation-property detection via a raw Roslyn
  re-parse of the source file (richer than the metadata's own `Classes`
  data), and one `{Entity}.g.cs` output per entity containing both an
  `ObservableObject` partial for the entity itself and its contribution to
  a shared `partial class MainModel`.