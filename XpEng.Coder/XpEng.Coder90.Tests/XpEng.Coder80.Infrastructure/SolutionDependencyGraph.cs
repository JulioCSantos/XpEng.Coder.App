using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace XpEng.Coder90.Tests.XpEng.Coder80.Infrastructure;

/// <summary>
/// One project in the solution dependency graph.
///
/// Everything a project can know about itself is derived lazily from ProjectFilePath, so a
/// node is cheap to create and only reads its .csproj if something actually asks. The three
/// graph-level properties — Dependencies, Parents and Tier — are deliberately NOT lazy: a
/// node cannot resolve a reference without the full project dictionary to match names
/// against, and cannot know its tier without the whole graph. Those are set by the graph.
/// </summary>
public sealed class ProjectNode {

    #region properties
    /// <summary>The one value everything else on this node derives from.</summary>
    public string ProjectFilePath { get; }

    /// <summary>Assembly/project name taken from the .csproj FILE name — never the containing
    /// folder, which can legitimately differ (e.g. folder "XpEng.Coder88.Infrastructure"
    /// holding "XpEng.Coder80.Infrastructure.csproj").</summary>
    public string ProjectName => field ??= Path.GetFileNameWithoutExtension(ProjectFilePath);

    public string ProjectDirectory => field ??= Path.GetDirectoryName(ProjectFilePath) ?? string.Empty;

    /// <summary>Declared RootNamespace, falling back to the project name when absent — the
    /// same default the SDK applies. Read rather than assumed, since the two can differ.</summary>
    public string RootNamespace => field ??= ReadRootNamespace();

    /// <summary>Detected structurally (an IsTestProject property or a known test-framework
    /// PackageReference), never by name convention.</summary>
    public bool IsTestProject {
        get {
            if (!_isTestProjectResolved) { field = ReadIsTestProject(); _isTestProjectResolved = true; }
            return field;
        }
    }
    private bool _isTestProjectResolved;

    /// <summary>Number embedded in the project name when the solution uses a numbering
    /// convention (e.g. "XpEng.Coder09.Models" -> 9); null otherwise. Purely informational —
    /// nothing in the tier algorithm reads it. Needs an explicit resolved flag rather than
    /// ??= because null is a legitimate result, not "not yet computed".</summary>
    public int? UserTierNumber {
        get {
            if (!_userTierNumberResolved) { field = ParseUserTierNumber(ProjectName); _userTierNumberResolved = true; }
            return field;
        }
    }
    private bool _userTierNumberResolved;

    /// <summary>ProjectDirectory relative to the solution root, so stored values survive the
    /// repository moving between machines or drive letters. Graph-set: the solution root this
    /// is measured against belongs to the graph, not the node.</summary>
    public string RelativeDirectory { get; internal set; } = string.Empty;

    /// <summary>Graph-computed tier. Solution root is conceptually 0; entry points are 1,
    /// increasing downward. Null when unresolved (a dependency cycle).</summary>
    public int? Tier { get; internal set; }

    /// <summary>Projects this one references directly.</summary>
    public List<ProjectNode> Dependencies { get; } = new();

    /// <summary>Projects that reference this one. Maintained explicitly because tier phase 1
    /// walks the graph in this direction.</summary>
    public List<ProjectNode> Parents { get; } = new();

    /// <summary>Parsed .csproj, loaded once and shared by every property that reads it. Null
    /// when the file could not be read; callers fall back to sensible defaults.</summary>
    private XDocument? ProjectDocument {
        get {
            if (_projectDocumentLoaded) return field;
            _projectDocumentLoaded = true;
            try { field = XDocument.Load(ProjectFilePath); }
            catch { field = null; }
            return field;
        }
    }
    private bool _projectDocumentLoaded;

    /// <summary>True when the .csproj could not be read at all.</summary>
    internal bool ProjectFileUnreadable => ProjectDocument == null;

    private static readonly string[] TestFrameworkPackageMarkers = {
        "Microsoft.NET.Test.Sdk", "xunit", "xunit.runner.visualstudio", "NUnit",
        "MSTest.TestFramework", "MSTest.TestAdapter"
    };
    #endregion properties

    #region constructors
    internal ProjectNode(string projectFilePath) {
        ProjectFilePath = projectFilePath;
    }
    #endregion constructors

    #region methods
    private string ReadRootNamespace() {
        string? declared = ProjectDocument?.Descendants("RootNamespace").FirstOrDefault()?.Value;
        return string.IsNullOrWhiteSpace(declared) ? ProjectName : declared.Trim();
    }

    private bool ReadIsTestProject() {
        var document = ProjectDocument;
        if (document == null) return false;

        string? flag = document.Descendants("IsTestProject").FirstOrDefault()?.Value;
        if (bool.TryParse(flag, out bool isTest) && isTest) return true;

        foreach (var package in document.Descendants("PackageReference")) {
            string? id = package.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (TestFrameworkPackageMarkers.Any(m => id.Equals(m, StringComparison.OrdinalIgnoreCase))) return true;
        }

        return false;
    }

    /// <summary>Every ProjectReference Include value, exactly as written in the .csproj. The
    /// graph resolves these to nodes; the node itself cannot, having no view of its siblings.</summary>
    internal IEnumerable<string> ReadProjectReferenceIncludes() {
        var document = ProjectDocument;
        if (document == null) yield break;

        foreach (var reference in document.Descendants("ProjectReference")) {
            string? include = reference.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(include)) yield return include;
        }
    }

    private static int? ParseUserTierNumber(string projectName) {
        foreach (string segment in projectName.Split('.')) {
            var match = Regex.Match(segment, @"(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int value)) return value;
        }
        return null;
    }

    public override string ToString() => ProjectName;
    #endregion methods
}

/// <summary>
/// Builds a project dependency graph by reading the .slnx/.sln for the project list and each
/// .csproj for its ProjectReference edges — MSBuild's own files, so the graph cannot drift
/// from what actually builds.
///
/// Tiers are computed in two phases:
///
///   Phase 1 — every project sits one below the DEEPEST project that references it (longest
///   path, not shortest). Views referencing both ViewModels and Models directly is the case
///   that requires this: since ViewModels also references Models, Models belongs below
///   ViewModels rather than beside it.
///
///   Phase 2 — a project nothing references is initially treated as an entry point at tier 1,
///   which is wrong for something like a Data project that simply has no dependents. Phase 2
///   pulls each such project down to sit just above its SHALLOWEST dependency, so Data
///   depending on Models (3) lands at 2 rather than staying at 1.
/// </summary>
public sealed class SolutionDependencyGraph {

    #region properties
    public string SolutionFilePath { get; }
    public string SolutionDirectory { get; }

    /// <summary>The graph: project name -> node. Each node's Dependencies list is the value
    /// side of the "project name -> list of dependency nodes" shape.</summary>
    public Dictionary<string, ProjectNode> Projects { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Problems found while building — a missing .csproj, an unresolvable reference,
    /// or a dependency cycle. Non-fatal, but output built on an incomplete graph is suspect.</summary>
    public List<string> Diagnostics { get; } = new();
    #endregion properties

    #region constructors
    public SolutionDependencyGraph(string solutionFilePath) {
        if (string.IsNullOrWhiteSpace(solutionFilePath)) throw new ArgumentException("Solution file path is required.", nameof(solutionFilePath));
        if (!File.Exists(solutionFilePath)) throw new FileNotFoundException("Solution file not found.", solutionFilePath);

        SolutionFilePath = Path.GetFullPath(solutionFilePath);
        SolutionDirectory = Path.GetDirectoryName(SolutionFilePath) ?? string.Empty;

        BuildGraph();
    }
    #endregion constructors

    #region build
    private void BuildGraph() {
        foreach (string projectPath in ReadProjectPaths()) {
            if (!File.Exists(projectPath)) {
                Diagnostics.Add($"Project listed in the solution was not found: {projectPath}");
                continue;
            }

            var node = new ProjectNode(projectPath);
            if (Projects.ContainsKey(node.ProjectName)) {
                Diagnostics.Add($"Duplicate project name '{node.ProjectName}' — only the first is kept.");
                continue;
            }
            Projects[node.ProjectName] = node;
        }

        foreach (var node in Projects.Values) {
            node.RelativeDirectory = GetRelativePath(SolutionDirectory, node.ProjectDirectory);
            ResolveDependencies(node);
        }

        ComputeTiersPhase1();
        ComputeTiersPhase2();
    }

    /// <summary>Both .slnx and legacy .sln carry only a flat project list — no hierarchy —
    /// which is why the edges must come from the .csproj files.</summary>
    private List<string> ReadProjectPaths() {
        var paths = new List<string>();

        if (Path.GetExtension(SolutionFilePath).Equals(".slnx", StringComparison.OrdinalIgnoreCase)) {
            var doc = XDocument.Load(SolutionFilePath);
            foreach (var element in doc.Descendants("Project")) {
                string? relative = element.Attribute("Path")?.Value;
                if (string.IsNullOrWhiteSpace(relative)) continue;
                paths.Add(Path.GetFullPath(Path.Combine(SolutionDirectory, relative.Replace('/', Path.DirectorySeparatorChar))));
            }
        }
        else {
            foreach (string line in File.ReadAllLines(SolutionFilePath)) {
                var match = Regex.Match(line, @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*""\s*,\s*""([^""]+\.csproj)""", RegexOptions.IgnoreCase);
                if (!match.Success) continue;
                paths.Add(Path.GetFullPath(Path.Combine(SolutionDirectory, match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar))));
            }
        }

        return paths;
    }

    /// <summary>Resolves a node's ProjectReference includes to sibling nodes, wiring both edge
    /// directions. Only the graph can do this — a node has no view of its siblings.</summary>
    private void ResolveDependencies(ProjectNode node) {
        if (node.ProjectFileUnreadable) {
            Diagnostics.Add($"Could not read project file: {node.ProjectFilePath}");
            return;
        }

        foreach (string include in node.ReadProjectReferenceIncludes()) {
            string referencedPath = Path.GetFullPath(Path.Combine(node.ProjectDirectory, include.Replace('\\', Path.DirectorySeparatorChar)));
            string referencedName = Path.GetFileNameWithoutExtension(referencedPath);

            if (!Projects.TryGetValue(referencedName, out var referenced)) {
                Diagnostics.Add($"'{node.ProjectName}' references a project not listed in the solution: {include}");
                continue;
            }

            if (!node.Dependencies.Contains(referenced)) node.Dependencies.Add(referenced);
            if (!referenced.Parents.Contains(node)) referenced.Parents.Add(node);
        }
    }

    /// <summary>Phase 1: tier = deepest parent + 1. Iterative rather than recursive so a cycle
    /// cannot overflow the stack — it simply fails to converge, which is then reported.</summary>
    private void ComputeTiersPhase1() {
        var production = Projects.Values.Where(p => !p.IsTestProject).ToList();
        if (production.Count == 0) return;

        foreach (var node in production) node.Tier = HasNoProductionParent(node) ? 1 : null;

        int maxIterations = production.Count + 1;
        bool changed = true;

        for (int i = 0; changed && i < maxIterations; i++) {
            changed = false;
            foreach (var node in production) {
                var parents = node.Parents.Where(p => !p.IsTestProject).ToList();
                if (parents.Count == 0) continue;
                if (parents.Any(p => p.Tier == null)) continue;

                int candidate = parents.Max(p => p.Tier!.Value) + 1;
                if (node.Tier != candidate) {
                    node.Tier = candidate;
                    changed = true;
                }
            }
        }

        var unresolved = production.Where(p => p.Tier == null).ToList();
        if (unresolved.Count > 0) {
            Diagnostics.Add("Dependency cycle detected — tiers unresolved for: " +
                            string.Join(", ", unresolved.Select(p => p.ProjectName)));
        }
    }

    /// <summary>Phase 2: pull each parentless project down to sit just above its shallowest
    /// dependency. Cannot cascade — a parentless project is never the deepest parent of
    /// anything, so moving it cannot invalidate a child's phase 1 placement.</summary>
    private void ComputeTiersPhase2() {
        foreach (var node in Projects.Values.Where(p => !p.IsTestProject)) {
            if (!HasNoProductionParent(node)) continue;

            var dependencies = node.Dependencies.Where(d => !d.IsTestProject && d.Tier.HasValue).ToList();
            if (dependencies.Count == 0) continue;

            int adjusted = dependencies.Min(d => d.Tier!.Value) - 1;
            if (node.Tier.HasValue && adjusted > node.Tier.Value) node.Tier = adjusted;
        }
    }

    /// <summary>Referenced by nothing except (possibly) test projects — so a production project
    /// referenced only by its own test project still counts as parentless.</summary>
    private static bool HasNoProductionParent(ProjectNode node) => node.Parents.All(p => p.IsTestProject);
    #endregion build

    #region search
    public ProjectNode? FindByName(string projectName, bool includeTestProjects = false) =>
        Candidates(includeTestProjects).FirstOrDefault(p => p.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Wildcard search, e.g. "*.Views" or "*Infrastructure*".</summary>
    public List<ProjectNode> FindByPattern(string pattern, bool includeTestProjects = false) {
        string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Candidates(includeTestProjects).Where(p => Regex.IsMatch(p.ProjectName, regex, RegexOptions.IgnoreCase)).ToList();
    }

    /// <summary>Every project at a computed tier. More than one is normal.</summary>
    public List<ProjectNode> FindByTier(int tier, bool includeTestProjects = false) =>
        Candidates(includeTestProjects).Where(p => p.Tier == tier).ToList();

    /// <summary>Projects whose name carries a given convention number. Empty when the solution
    /// uses no numbering convention.</summary>
    public List<ProjectNode> FindByUserTierNumber(int userTierNumber, bool includeTestProjects = false) =>
        Candidates(includeTestProjects).Where(p => p.UserTierNumber == userTierNumber).ToList();

    /// <summary>Test projects, which every other search excludes by default.</summary>
    public List<ProjectNode> FindTestProjects() => Projects.Values.Where(p => p.IsTestProject).ToList();

    /// <summary>Everything this project reaches, directly or transitively.</summary>
    public List<ProjectNode> GetAllDependencies(ProjectNode node) {
        var seen = new HashSet<ProjectNode>();
        var queue = new Queue<ProjectNode>(node.Dependencies);
        while (queue.Count > 0) {
            var current = queue.Dequeue();
            if (!seen.Add(current)) continue;
            foreach (var dependency in current.Dependencies) queue.Enqueue(dependency);
        }
        return seen.ToList();
    }

    private IEnumerable<ProjectNode> Candidates(bool includeTestProjects) =>
        includeTestProjects ? Projects.Values : Projects.Values.Where(p => !p.IsTestProject);
    #endregion search

    #region helpers
    private static string GetRelativePath(string basePath, string targetPath) {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(targetPath)) return targetPath;
        string relative = Path.GetRelativePath(basePath, targetPath);
        return relative == "." ? string.Empty : relative;
    }
    #endregion helpers
}

/// <summary>
/// Display helpers for visual inspection of the graph.
/// </summary>
public static class SolutionDependencyGraphExtensions {

    /// <summary>Flat listing ordered by tier, with both edge directions — the quickest way to
    /// eyeball whether tiers came out as expected.</summary>
    public static string Describe(this SolutionDependencyGraph graph) {
        var sb = new StringBuilder();

        sb.AppendLine($"Solution : {Path.GetFileName(graph.SolutionFilePath)}");
        sb.AppendLine($"Root     : {graph.SolutionDirectory}");
        sb.AppendLine($"Projects : {graph.Projects.Count} ({graph.FindTestProjects().Count} test)");
        sb.AppendLine();

        foreach (var node in graph.Projects.Values.OrderBy(p => p.Tier ?? int.MaxValue).ThenBy(p => p.ProjectName)) {
            string tier = node.Tier?.ToString() ?? "-";
            string userTier = node.UserTierNumber?.ToString() ?? "-";
            string marker = node.IsTestProject ? "  [TEST]" : string.Empty;

            sb.AppendLine($"[Tier {tier}] {node.ProjectName}{marker}");
            sb.AppendLine($"    UserTier#  : {userTier}");
            sb.AppendLine($"    Namespace  : {node.RootNamespace}");
            sb.AppendLine($"    Directory  : {node.RelativeDirectory.Replace('\\', '/')}");
            sb.AppendLine($"    Depends on : {(node.Dependencies.Count == 0 ? "(none)" : string.Join(", ", node.Dependencies.Select(d => d.ProjectName)))}");
            sb.AppendLine($"    Parents    : {(node.Parents.Count == 0 ? "(none)" : string.Join(", ", node.Parents.Select(p => p.ProjectName)))}");
            sb.AppendLine();
        }

        if (graph.Diagnostics.Count > 0) {
            sb.AppendLine("DIAGNOSTICS:");
            foreach (string diagnostic in graph.Diagnostics) sb.AppendLine($"  - {diagnostic}");
        }

        return sb.ToString();
    }

    /// <summary>Indented tree from each entry point downward. A project reachable by several
    /// paths appears more than once; a cycle is cut short and marked.</summary>
    public static string DescribeTree(this SolutionDependencyGraph graph) {
        var sb = new StringBuilder();
        sb.AppendLine($"{Path.GetFileNameWithoutExtension(graph.SolutionFilePath)}  [Tier 0]");

        var roots = graph.Projects.Values
            .Where(p => !p.IsTestProject && p.Tier == 1)
            .OrderBy(p => p.ProjectName)
            .ToList();

        foreach (var root in roots) AppendBranch(sb, root, 1, new HashSet<ProjectNode>());

        var orphans = graph.Projects.Values
            .Where(p => !p.IsTestProject && p.Tier != 1 && p.Parents.All(x => x.IsTestProject))
            .OrderBy(p => p.Tier)
            .ToList();

        if (orphans.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("Not referenced by any project (placed just above their shallowest dependency):");
            foreach (var orphan in orphans) AppendBranch(sb, orphan, 1, new HashSet<ProjectNode>());
        }

        var tests = graph.FindTestProjects();
        if (tests.Count > 0) {
            sb.AppendLine();
            sb.AppendLine("Test projects (excluded from tier computation):");
            foreach (var test in tests.OrderBy(t => t.ProjectName))
                sb.AppendLine($"  {test.ProjectName} -> {string.Join(", ", test.Dependencies.Select(d => d.ProjectName))}");
        }

        return sb.ToString();
    }

    private static void AppendBranch(StringBuilder sb, ProjectNode node, int depth, HashSet<ProjectNode> path) {
        string indent = new string(' ', depth * 4);

        if (!path.Add(node)) {
            sb.AppendLine($"{indent}{node.ProjectName}  [cycle]");
            return;
        }

        sb.AppendLine($"{indent}{node.ProjectName}  [Tier {node.Tier?.ToString() ?? "-"}]");
        foreach (var dependency in node.Dependencies.Where(d => !d.IsTestProject).OrderBy(d => d.ProjectName))
            AppendBranch(sb, dependency, depth + 1, path);

        path.Remove(node);
    }
}