namespace XpEng.Coder80.Infrastructure.Extensions {
    public static class StringExtensions {
        public const int DefaultSegmentCount = 4;

        public static string Shorten(string? fullPath, int segmentCount = DefaultSegmentCount) {
            if (string.IsNullOrWhiteSpace(fullPath)) return string.Empty;

            var segments = fullPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length <= segmentCount) return fullPath;

            var tail = segments.Skip(segments.Length - segmentCount);
            return @"...\" + string.Join(@"\", tail);
        }
    }
}
