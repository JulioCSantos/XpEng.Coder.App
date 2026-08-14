# T4 Template Contract for XpEng.Coder.App

This document defines what a T4 template must do to be invoked correctly by
`XpEng.Coder.App`. Templates live in an arbitrary repository folder — not
necessarily inside any solution Coder.App watches — and are selected by an
operator at runtime, so this contract is the only thing tying a template to
the host correctly. `BasicClassTemplate.tt` is the reference implementation;
when in doubt, read it alongside this doc.

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
Never assume a non-`Deleted` `ChangeType` implies meaningful content.

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

## Constraints

- **Session values are strings only.** No object graphs, no custom types.
- **Assembly references must resolve outside any solution context.**
  Coder.App runs your template via a hosted engine, not MSBuild — a
  `<#@ assembly name="..." #>` reference needs to be in the GAC, sitting
  alongside Coder.App's own binaries, or given as an absolute path. A
  reference that only resolved because of a specific solution's build
  output won't work here.
- **Don't rely on state persisting between invocations.** The compiled
  template is reused across many batches over the app's lifetime; treat
  each invocation as a fresh, independent run and avoid depending on
  static or instance state left over from a previous batch.
- **Extension/format filtering is your responsibility.** Coder.App forwards
  every file change in a watched directory regardless of extension or
  content — if your template only cares about certain kinds of source
  files, check `SourceFileName` yourself inside the loop.
- **Output naming and layout are entirely up to you.** The
  `{SourceFileName}{Suffix}.cs` convention used by the example templates is
  just that — a convention, not a requirement.

## Minimal skeleton

```csharp
<#@ template language="C#" hostspecific="true" #>
<#@ assembly name="System.Text.Json" #>
<#@ import namespace="System.Text.Json" #>
<#@ import namespace="System.IO" #>
<#@ parameter name="MetadataFilePath" type="System.String" #>
<#@ parameter name="TargetDirectory" type="System.String" #>
<#
    if (string.IsNullOrWhiteSpace(MetadataFilePath) || !File.Exists(MetadataFilePath)) { Write("Error: MetadataFilePath is missing or invalid."); return string.Empty; }
    using var document = JsonDocument.Parse(File.ReadAllText(MetadataFilePath));
    var files = document.RootElement.GetProperty("Files");
    int count = 0;
    foreach (var fileEntry in files.EnumerateArray())
    {
        string sourceFileName = fileEntry.GetProperty("SourceFileName").GetString();
        string changeType = fileEntry.GetProperty("ChangeType").GetString();
        string outputPath = Path.Combine(TargetDirectory, $"{sourceFileName}.Generated.cs");

        if (changeType == "Deleted") { if (File.Exists(outputPath)) File.Delete(outputPath); continue; }

        // ...generate your content here, using fileEntry.GetProperty("Classes") as needed...
        File.WriteAllText(outputPath, "// generated content");
        count++;
    }
    Write($"Batch complete: {count} file(s) generated.");
#>
```

## Reference implementation

`BasicClassTemplate.tt` implements this full contract, including deletion
and rename handling — use it as the canonical working example.
