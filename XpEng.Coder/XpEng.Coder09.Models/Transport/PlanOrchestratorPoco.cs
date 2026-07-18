using System;
using System.Collections.Generic;
using System.Text;

namespace XpEng.Coder09.Models.Transport {
    public class PlanOrchestratorPoco {
        public string Id { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public List<TemplateTargetPoco> TemplateTargets { get; set; } = new();
    }
}
