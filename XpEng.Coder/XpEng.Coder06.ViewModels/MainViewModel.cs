using CommunityToolkit.Mvvm.ComponentModel;

namespace XpEng.Coder06.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable {

    private static readonly Lazy<MainViewModel> _instance = new(() => new MainViewModel());
    public static MainViewModel Instance => _instance.Value;

    protected MainViewModel() { } // Protected constructor for unit testing and to prevent external instantiation

    public CounterViewModel CounterViewModel  => field ??= new CounterViewModel();
    public DashboardViewModel DashboardViewModel => field ??= new DashboardViewModel();

    public void Dispose() {
        // Cascade the disposal down to the child ViewModel
        DashboardViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }
}