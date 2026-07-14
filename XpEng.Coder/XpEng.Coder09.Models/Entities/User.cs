namespace XpEng.Coder09.Models.Entities;

public class User {
    public required int Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}