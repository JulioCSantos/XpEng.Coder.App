using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace XpEng.Coder88.Infrastructure.WPF;

public abstract class SetPropertyBase : INotifyPropertyChanged
{
    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    [DebuggerStepThrough]
    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null) {
        if (propertyName != null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [DebuggerStepThrough]
    protected virtual void SetProperty<T>( ref T? backingField, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(value, backingField) || propertyName == null) return;

        backingField = value;

        RaisePropertyChanged(propertyName);
    }
    #endregion INotifyPropertyChanged
}
