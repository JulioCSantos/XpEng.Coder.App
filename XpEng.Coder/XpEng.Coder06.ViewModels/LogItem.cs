using CommunityToolkit.Mvvm.ComponentModel;

namespace XpEng.Coder06.ViewModels {
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