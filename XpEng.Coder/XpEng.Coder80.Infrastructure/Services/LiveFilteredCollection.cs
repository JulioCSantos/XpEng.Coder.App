using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TBQuiz80.Infrastructure.Services;

public sealed class LiveFilteredCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged {
    private readonly ObservableCollection<T> _source;
    private readonly Func<T, bool> _filter;
    private readonly HashSet<string> _watchedProperties;

    public LiveFilteredCollection(ObservableCollection<T> source, Func<T, bool> filter, params string[] watchedProperties) {
        _source = source;
        _filter = filter;
        _watchedProperties = new HashSet<string>(watchedProperties, StringComparer.Ordinal);

        foreach (var item in _source) {
            item.PropertyChanged += OnSourceItemPropertyChanged;
            if (_filter(item)) Add(item);
        }
        _source.CollectionChanged += OnSourceCollectionChanged;
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (e.NewItems != null) {
            foreach (T item in e.NewItems) {
                item.PropertyChanged += OnSourceItemPropertyChanged;
                if (_filter(item) && !Contains(item)) Add(item);
            }
        }
        if (e.OldItems != null) {
            foreach (T item in e.OldItems) {
                item.PropertyChanged -= OnSourceItemPropertyChanged;
                if (Contains(item)) Remove(item);
            }
        }
    }

    private void OnSourceItemPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is not T item) return;
        if (e.PropertyName != null && !_watchedProperties.Contains(e.PropertyName)) return;

        bool matches = _filter(item);
        bool currentlyIncluded = Contains(item);
        if (matches && !currentlyIncluded) Add(item);
        else if (!matches && currentlyIncluded) Remove(item);
    }
}