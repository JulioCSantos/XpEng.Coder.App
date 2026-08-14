namespace XpEng.Coder80.Infrastructure.T4Pipeline;

public sealed class T4GenerationRequest {
    public int RequestId { get; set; }
    public string TemplatePath { get; set; } = string.Empty;
    public string TemplateContentHash { get; set; } = string.Empty;
    public string TemplateContent { get; set; } = string.Empty;
    public Dictionary<string, string> SessionParameters { get; set; } = new();
    public string OutputFilePath { get; set; } = string.Empty;
}
