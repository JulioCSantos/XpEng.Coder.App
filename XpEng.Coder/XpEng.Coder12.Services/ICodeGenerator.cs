using System;
using System.Collections.Generic;

namespace XpEng.Coder12.Services {
    public interface ICodeGenerator {
        void FullSynchronization(string dataProjectDir, string targetDir, Action<string> logAction);
        void ProcessBatch(IEnumerable<FileChangeEvent> changes, string dataProjectDir, string targetDir, Action<string> logAction);
    }
}