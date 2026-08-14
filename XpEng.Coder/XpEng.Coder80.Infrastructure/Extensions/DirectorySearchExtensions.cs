using System;
using System.IO;
using System.Linq;

namespace XpEng.Coder80.Infrastructure.Extensions {
    public static class DirectorySearchExtensions {

        public static string? FindAncestorSolutionRoot(string? sourcePath) {
            if (string.IsNullOrWhiteSpace(sourcePath)) return null;

            try {
                var directory = new DirectoryInfo(sourcePath);
                while (directory != null && directory.Exists) {
                    bool hasSolutionFile = directory.EnumerateFiles("*.sln").Any() || directory.EnumerateFiles("*.slnx").Any();
                    if (hasSolutionFile) return directory.FullName;
                    directory = directory.Parent;
                }
            }
            catch {
                return null;
            }

            return null;
        }
    }
}