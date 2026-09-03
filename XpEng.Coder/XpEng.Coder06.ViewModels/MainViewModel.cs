using CommunityToolkit.Mvvm.ComponentModel;
using XpEng.Coder09.Models;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder06.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable {
    private MainModel? _mainModel;
    private DashboardViewModel? _dashboardViewModel;

    public static MainViewModel Instance => DesignTimeSingleton<MainViewModel>.Resolve(() => new MainViewModel());

    public MainModel MainModel => _mainModel ??= MainModel.Instance;


    protected internal MainViewModel() { }


    public DashboardViewModel DashboardViewModel => _dashboardViewModel ??= new DashboardViewModel();

    public void Dispose() {
        // Backing fields, not the properties: the properties are lazy, so touching one here
        // would construct the object purely in order to dispose it — during disposal, when the
        // container it would resolve from is already going away.
        //
        // MainModel is deliberately absent: this class resolves it from the container rather
        // than owning it, so disposing it here would tear it out from under every other holder.
        _dashboardViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }
}