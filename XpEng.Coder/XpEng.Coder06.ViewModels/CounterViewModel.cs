using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XpEng.Coder06.ViewModels;

public partial class CounterViewModel : ObservableObject {
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EvenOrOddMessage))]
    private int _count = 0;

    public string EvenOrOddMessage => Count % 2 == 0 ? "Even" : "Odd";

    [RelayCommand]
    private void Increment() {
        Count++;
    }
}