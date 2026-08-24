using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder03.Views;

internal static class DesignTimeModuleInitializer {
    [ModuleInitializer]
    internal static void Initialize() {
        DesignTimeDetector.IsInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
    }
}