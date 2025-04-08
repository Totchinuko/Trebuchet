using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace TrebuchetUtils;

public class TClasses : AvaloniaObject
{
    public static readonly AttachedProperty<ObservableCollection<string>> ClassesProperty =
        AvaloniaProperty.RegisterAttached<TClasses, Interactive, ObservableCollection<string>>("DynClasses");

    static TClasses()
    {
        ClassesProperty.Changed.AddClassHandler<Interactive>(OnClassesChanged);
    }

    private static void OnClassesChanged(Interactive sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.OldValue is ObservableCollection<string> collection)
            collection.CollectionChanged -= OnCollectionChanged;
        if (args.NewValue is ObservableCollection<string> newCollection)
        {
            newCollection.CollectionChanged += OnCollectionChanged;
            sender.Classes.Clear();
            sender.Classes.AddRange(newCollection);
        }
    }


    private static void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not Interactive interactive) return;
        interactive.Classes.Clear();
        interactive.Classes.AddRange(GetClasses(interactive));
    }

    public static void SetClasses(AvaloniaObject element, ObservableCollection<string> value)
    {
        element.SetValue(ClassesProperty, value);
    }

    public static ObservableCollection<string> GetClasses(AvaloniaObject element)
    {
        return element.GetValue(ClassesProperty);
    }
}