using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder06.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable {

    public static MainViewModel Instance => DesignTimeSingleton<MainViewModel>.Resolve(() => new MainViewModel());

    public MainModel MainModel => field ??= MainModel.Instance;


    protected internal MainViewModel() { }


    public DashboardViewModel DashboardViewModel => field ??= new DashboardViewModel();

    public void Dispose() {
        DashboardViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }
}