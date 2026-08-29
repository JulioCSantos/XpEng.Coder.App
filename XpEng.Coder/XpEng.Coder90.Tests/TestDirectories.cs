using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace XpEng.Coder90.Tests {
    public class TestDirectories {

        /// <summary>
        /// Walks upward from the test assembly's location until a folder containing a .slnx file is
        /// found, then returns the full path of the subfolder named <paramref name="folderName"/>
        /// inside it. Returns null if no .slnx is found before reaching the drive root, or if the
        /// named subfolder does not exist.
        /// </summary>
        public static string GetSlnxFolder(string folderName) {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null) {
                if (directory.GetFiles("*.slnx").Length > 0) {
                    string candidate = Path.Combine(directory.FullName, folderName);
                    Directory.CreateDirectory(candidate);
                    return candidate;
                }
                directory = directory.Parent;
            }
            throw new InvalidOperationException($"No .slnx found above {AppContext.BaseDirectory}");
        }
    }
}