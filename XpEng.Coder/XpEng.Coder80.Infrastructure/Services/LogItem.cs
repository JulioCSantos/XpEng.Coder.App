using CommunityToolkit.Mvvm.ComponentModel;

namespace XpEng.Coder80.Infrastructure.Services {
    public partial class LogItem : ObservableObject {
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private string _message;

        public LogItem(string message) {
            Message = message;
        }
    }
}