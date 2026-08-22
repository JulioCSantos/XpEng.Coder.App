# T4 Template Contract for XpEng.Coder.App

This document defines what a T4 template must do to be invoked correctly by
`XpEng.Coder.App`. Templates live in an arbitrary repository folder — not
necessarily inside any solution Coder.App watches — and are selected by an
operator at runtime, so this contract is the only thing tying a template to
the host correctly. `BasicClassTemplate.tt` and `ModelsProject.tt` are the
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

`File.WriteAllText` always fully truncates before writing — there is no
merge or append between an old and new version of a generated file. Every
regeneration replaces 100% of a file's prior content. The one case this
doesn't protect against: if a template's *output naming scheme* itself
changes between versions, files written under the old naming become
orphans, since nothing revisits a path the current template no longer
computes. Clean these up manually (or, since operators typically target an
empty, dedicated output folder for exactly this reason, by clearing that
folder before a full regeneration).

## File filters

Every template should declare its own filter rules at the very top of the
generation block — the one place a maintainer needs to read or edit to
change what the template acts on. Rules are OR'd together; within one rule,
every set criterion must match (AND):

```csharp
var Filters = new[] {
    new FileFilterRule {
        NamePatterns = new[] { "*.cs" },
        ExcludeNamePatterns = new[] { "*.Designer.cs", "*.g.cs" },
        ExcludedBaseTypes = new[] { "DbContext" }
    }
};
```

`FileFilterRule` supports name patterns (wildcard, `*`/`?`), exclude
patterns (checked after name patterns, any match rejects), `ChangeTypes`,
and required/excluded base-type checks.

**Prefer type-derivation checks (`RequiredBaseType`/`ExcludedBaseTypes`)
over filename patterns whenever the real distinguishing signal is what a
class *is*, not what its file is *named*.** A filename-based rule like
`"*DbContext.cs"` is fragile in two specific ways worth knowing before you
reach for it: it can only match a suffix you thought to enumerate, and it
says nothing about the class the file actually declares. A concrete case
that broke this exact way: EF Core Power Tools can split a `DbContext`
across a main file plus companion partial files (e.g.
`{Context}.Functions.cs`) — a filename pattern anchored to end in
`DbContext.cs` never matches the companion file at all, so it slips
through and gets processed as if it were a domain entity.

Type-derivation checks require a Roslyn parse — genuinely more expensive
than a filename check, which is why they're only evaluated if a rule
actually sets `RequiredBaseType`/`ExcludedBaseTypes`. But there's a real
subtlety here too, worth understanding rather than copying blind:

- **A single file's own class declaration isn't enough to determine its
  base type, if the class is `partial`.** C# only requires *one* partial
  declaration in a group to state the base type — a companion partial file
  can (and typically does) omit it entirely. Checking one file in
  isolation would report the companion file as deriving from nothing at
  all, missing the exact case that motivated switching to type-derivation
  checking in the first place. The fix: build a directory-wide index,
  mapping each class *name* to the union of every base type any of its
  partial declarations mentions anywhere in the source directory — then
  resolve any single file's own class name against that merged index, not
  against its own text alone.
- **Multi-level inheritance needs the chain walked, not just one hop.** A
  class deriving from an intermediate base — which itself derives from
  `DbContext` — won't have `"DbContext"` anywhere in its own immediate
  base-type list. The check needs to walk transitively: resolve the
  immediate base, then (if that name is itself a locally-declared class)
  resolve *its* base, and so on, until either the target type name is
  found somewhere in the chain, or the trail runs out at a name that isn't
  declared locally (which is exactly what happens the moment the walk
  reaches something like the real `DbContext` — it comes from a NuGet
  package, not from any file in the directory, so there's nothing further
  to resolve, and the walk correctly stops there).

See `GetBaseTypeIndex`/`GetAllAncestorTypes`/`GetBaseTypesForFile` in
`ModelsProject.tt` for the full working implementation — the directory
index is built once per source directory and cached, so this doesn't mean
re-scanning the whole directory for every file checked.

One known limitation, not currently handled: this matches base-type names
by the exact text written in source (e.g. `"DbContext"`). A fully-qualified
base type written as `Microsoft.EntityFrameworkCore.DbContext` instead of
relying on a `using` directive would not match. Every EF Core Power Tools
sample encountered so far uses the short form consistently; treat this as
a low-risk simplification, not a guarantee.

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
4. **Exception:** strip the leaf folder segment before joining, in either
   of two cases — (a) it's literally named `AutoGenerated` or `Generated`
   (case-insensitive), or (b) it case-insensitively duplicates the
   *trailing segment* of the project's own root namespace (e.g. a project
   named `TBQuiz09.Models` with a subfolder also named `Models` — left
   unhandled, this produces a doubled namespace like
   `TBQuiz09.Models.Models`, which silently breaks implicit access to
   sibling types declared directly in the project's real namespace). Case
   (a) is an explicit, deliberate convention; case (b) is a defensive
   generalization that catches the same class of mistake for *any*
   project/folder-naming combination, not just ones following the
   `AutoGenerated`/`Generated` convention by name.

This lets generated code share a namespace with hand-written code sitting
one level up, while still living in its own, physically segregated folder
— required for partial classes, which must match namespace exactly but
don't need to share a directory.

See `ComputeNamespace`/`GetRelativePathManual` in either reference template
for the full implementation (`Path.GetRelativePath` is unavailable in this
compile context — use the `Uri`-based manual implementation instead, not
the BCL method).

### A single generated file can (and often should) declare more than one namespace block

File-scoped namespace syntax (`namespace X;`) only allows **one** namespace
declaration per file — if a generated file needs to contain types that
belong in genuinely different namespaces, switch to the older braced-block
form (`namespace X { ... }`) so multiple blocks can coexist in one file.

`ModelsProject.tt` is the concrete example: each generated file contains
both an entity's own class *and* that entity's contribution to a shared
`MainModel` type. These two pieces have different namespace requirements
that must **not** be conflated:

- The entity's own class uses the computed namespace above — it can
  legitimately live in a project subfolder distinct from other
  hand-written extensions of that same entity.
- `MainModel`'s contribution must **always** target the project's raw root
  namespace (from step 1–2 above, with none of the subfolder-collapsing
  logic applied) — regardless of which subfolder any given entity's own
  `TargetDirectory` happens to point at. `MainModel` is one fixed,
  canonical type; tying its namespace to a per-entity computed value that
  could vary between entities (or between a stale and freshly-regenerated
  file) risks producing multiple, disconnected types that only *look*
  identical, silently splitting `MainModel` into an ambiguous pair of
  unrelated types sharing one name.

When two blocks in the same file target different namespaces this way, the
block whose content references types declared in the *other* block's
namespace needs its own scoped `using` directive — placed inside that
specific namespace block, not at file scope — since one block doesn't
automatically see into an unrelated sibling block's namespace. See how
`ModelsProject.tt` adds `using {entityNamespace};` inside `MainModel`'s own
block for the working example.

## Output file naming

Generated files use a `.g.cs` suffix on the **file name only** — e.g.
`Drug.g.cs`, `DrugViewModel.g.cs`. This is a naming convention for
readability and tooling, not a mechanism for avoiding type collisions —
C# type identity is `namespace + type name`, and the file's name plays no
part in that. Collision avoidance comes from the combination of a
segregated output folder and the computed-namespace rule above; if a
genuine collision does happen, the expectation is that the generated file
is simply overwritten with the new content, not deduplicated or renamed.

## Nullable reference type context — declare it explicitly, per file

Any generated code using `?` on a reference type (`string?`,
`SomeEntity?`, `ObservableCollection<T>?`) needs an explicit nullable
context, or the compiler raises `CS8632` ("annotation... should only be
used in a '#nullable' annotations context"). Don't rely on the target
project's own `<Nullable>` csproj setting — it may be inconsistent, may
differ between projects in the same solution, or may change independently
of anything this template controls. Stamp `#nullable enable` directly into
every generated file's own header instead, right after the
`<auto-generated>` banner and before any `using` statements — the same
convention EF Core Power Tools itself uses in its own scaffolded output,
for the same reason: each file becomes self-contained and correct
regardless of whatever the project happens to be configured to.

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
  built so far. Start from the reference block in `ModelsProject.tt` if
  your template does anything beyond basic string/JSON work, and expect to
  add more if you hit `CS0012`/`CS0234` compile errors — `Path` also lacks
  some newer members (`GetRelativePath`) in this profile; prefer manual
  `Uri`-based implementations over assuming a BCL method is available.
- **C# does not support nested block comments.** If generated output needs
  to show a suggested/example line of code that itself contains a
  `/* placeholder */`-style comment, never wrap that whole line in an
  outer `/* ... */` too — the first `*/` encountered closes the *outer*
  comment early, and everything after it becomes live code, typically
  producing a cluster of confusing, seemingly-unrelated parser errors
  (`Argument missing`, `Invalid expression term '/'`, `; expected`,
  `} expected`) rather than one clear one. Use a single-line `//` comment
  for the whole suggested example instead — `//` comments don't care what
  `/*`/`*/` tokens appear within them, so there's no nesting risk at all.
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
        File.WriteAllText(outputPath, "#nullable enable\n\n// generated content");
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

This skeleton's `getBaseTypes` callback is a no-op stub (`() => new
List<string>()`) — copy the real `GetBaseTypeIndex`/`GetAllAncestorTypes`
implementation from `ModelsProject.tt` if your template actually needs
`RequiredBaseType`/`ExcludedBaseTypes` to work correctly, especially against
partial classes or multi-level inheritance.

## Reference implementations

- **`BasicClassTemplate.tt`** — one generated ViewModel per changed source
  file, including deletion and rename handling.
- **`ModelsProject.tt`** — the fullest working example: full scalar-property
  mirroring, foreign-key/navigation-property detection via a raw Roslyn
  re-parse of the source file (richer than the metadata's own `Classes`
  data), the dual-namespace-block pattern for entity + shared `MainModel`
  contribution in one file, `#nullable enable` per file, and the complete
  working `GetBaseTypeIndex`/`GetAllAncestorTypes` type-derivation
  implementation (partial-class-aware, transitive across multi-level
  inheritance).
