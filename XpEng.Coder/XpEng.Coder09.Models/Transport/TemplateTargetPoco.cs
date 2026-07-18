using System;
using System.Collections.Generic;
using System.Text;

namespace XpEng.Coder09.Models.Transport {
    public class TemplateTargetPoco {
        public string Id { get; set; } = string.Empty;
        public string TargetDirectory { get; set; } = string.Empty;
        public string TemplatePath { get; set; } = string.Empty;
        public bool IsMonitored { get; set; }
    }
}
