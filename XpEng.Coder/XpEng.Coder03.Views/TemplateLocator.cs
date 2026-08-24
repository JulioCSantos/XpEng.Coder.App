using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using XpEng.Coder06.ViewModels;
using XpEng.Coder03.Views.Views;

namespace XpEng.Coder03.Views;

public class TemplateLocator : DataTemplateSelector {
    // Dictionary to hold your C# mappings
    private readonly Dictionary<Type, Type> _mappings = new();

    // The public parameterless constructor required by XAML
    public TemplateLocator() {
        // Register individual View/ViewModel mappings here
        RegisterMapping<DashboardViewModel, DashboardView>();

        // Add future mappings here...
        // RegisterMapping<SettingsViewModel, SettingsView>();
    }

    private void RegisterMapping<TViewModel, TView>()
        where TViewModel : class
        where TView : UIElement {
        _mappings[typeof(TViewModel)] = typeof(TView);
    }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container) {
        if (item == null) return base.SelectTemplate(item, container);

        // If the ViewModel type is registered, generate the DataTemplate on the fly
        if (_mappings.TryGetValue(item.GetType(), out Type? viewType)) {
            return new DataTemplate(item.GetType()) {
                VisualTree = new FrameworkElementFactory(viewType)
            };
        }

        return base.SelectTemplate(item, container);
    }
}