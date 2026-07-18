using System.ComponentModel.DataAnnotations;
using System.IO;

namespace XpEng.Coder09.Models.Validation {
    public class PathSyntaxAttribute : ValidationAttribute {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is not string path || string.IsNullOrWhiteSpace(path)) return ValidationResult.Success;
            
            // Check for illegal characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) {
                return new ValidationResult("Path contains invalid characters.");
            }

            // Ensure it is a fully qualified absolute path (e.g., C:\Folder)
            if (!Path.IsPathRooted(path)) {
                return new ValidationResult("Path must be an absolute, rooted path.");
            }

            return ValidationResult.Success;
        }
    }
}