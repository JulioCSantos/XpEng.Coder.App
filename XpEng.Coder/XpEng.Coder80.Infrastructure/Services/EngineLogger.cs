using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using XpEng.Coder80.Infrastructure.Interfaces;

namespace XpEng.Coder80.Infrastructure.Services {
    [Register(typeof(IEngineLogger))]
    public class EngineLogger : IEngineLogger {

        #region Properties
        private readonly SynchronizationContext _syncContext;
        public ObservableCollection<LogItem> LiveLogs { get; } = [ ];
        #endregion Properties

        #region Constructors
        public EngineLogger() {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            Log("Engine logger initialized.");
        }
        #endregion Constructors

        #region Event Handlers & Methods
        public void Log(string message) {
            _syncContext.Post(_ => {
                LiveLogs.Add(new LogItem($"[{DateTime.Now:HH:mm:ss}] {message}"));
            }, null);
        }
        #endregion Event Handlers & Methods
    }
}