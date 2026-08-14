using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder80.Infrastructure.Interfaces {
    public interface IEngineLogger {
        ObservableCollection<LogItem> LiveLogs { get; }
        void Log(string message);
    }
}
