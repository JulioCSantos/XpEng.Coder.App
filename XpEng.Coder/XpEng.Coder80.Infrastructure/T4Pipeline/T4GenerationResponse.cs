namespace XpEng.Coder80.Infrastructure.T4Pipeline; 
public sealed class T4GenerationResponse {
    public int RequestId { get; set; }
    public bool Success { get; set; }
    public string? GeneratedContent { get; set; }
    public string? ErrorMessage { get; set; }
    public long ElapsedMilliseconds { get; set; }
}
