using System;
using System.Collections.Generic;
using System.Text;

namespace XpEng.Coder80.Infrastructure.Extensions {
    public static class PathExtensions {

        public static string GetDirectoryTail(this string path, int levels = 3, string separator = "-") {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            DirectoryInfo? dirInfo = new DirectoryInfo(path);
            var tailSegments = new List<string>();

            for (int i = 0; i < levels && dirInfo != null; i++) {
                tailSegments.Insert(0, dirInfo.Name);
                dirInfo = dirInfo.Parent;
            }

            return string.Join(separator, tailSegments);
        }
    }
}
