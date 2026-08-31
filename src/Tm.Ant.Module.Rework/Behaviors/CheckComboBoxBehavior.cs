using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

using HandyControl.Controls;

namespace Tm.Ant.Module.Rework.Behaviors;

public static class CheckComboBoxBehavior
{
    public static readonly DependencyProperty BindableSelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "BindableSelectedItems",
            typeof(IList),
            typeof(CheckComboBoxBehavior),
            new FrameworkPropertyMetadata(null, OnBindableSelectedItemsChanged));

    public static IList GetBindableSelectedItems(DependencyObject obj)
    {
        return (IList)obj.GetValue(BindableSelectedItemsProperty);
    }

    public static void SetBindableSelectedItems(DependencyObject obj, IList value)
    {
        obj.SetValue(BindableSelectedItemsProperty, value);
    }

    private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CheckComboBox checkComboBox)
        {
            // Unsubscribe from old collection
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= (sender, args) => OnCollectionChanged(checkComboBox, args);
            }

            // Subscribe to new collection
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += (sender, args) => OnCollectionChanged(checkComboBox, args);

                // Initial sync: ViewModel -> Control
                SyncViewModelToControl(checkComboBox, (IList)e.NewValue);
            }

            // Subscribe to control's selection changes
            checkComboBox.SelectionChanged -= OnCheckComboBoxSelectionChanged;
            checkComboBox.SelectionChanged += OnCheckComboBoxSelectionChanged;
        }
    }

    private static void OnCheckComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CheckComboBox checkComboBox)
        {
            var boundCollection = GetBindableSelectedItems(checkComboBox);
            if (boundCollection == null) return;

            // Sync Control -> ViewModel
            if (boundCollection is IList list)
            {
                // Remove deselected items
                foreach (var item in e.RemovedItems)
                {
                    if (list.Contains(item))
                    {
                        list.Remove(item);
                    }
                }

                // Add newly selected items
                foreach (var item in e.AddedItems)
                {
                    if (!list.Contains(item))
                    {
                        list.Add(item);
                    }
                }
            }
        }
    }

    private static void OnCollectionChanged(CheckComboBox checkComboBox, NotifyCollectionChangedEventArgs args)
    {
        // Sync ViewModel -> Control
        if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
        {
            foreach (var item in args.NewItems)
            {
                if (!checkComboBox.SelectedItems.Contains(item))
                {
                    checkComboBox.SelectedItems.Add(item);
                }
            }
        }
        else if (args.Action == NotifyCollectionChangedAction.Remove && args.OldItems != null)
        {
            foreach (var item in args.OldItems)
            {
                if (checkComboBox.SelectedItems.Contains(item))
                {
                    checkComboBox.SelectedItems.Remove(item);
                }
            }
        }
        else if (args.Action == NotifyCollectionChangedAction.Reset)
        {
            checkComboBox.SelectedItems.Clear();
        }
    }

    private static void SyncViewModelToControl(CheckComboBox checkComboBox, IList viewModelCollection)
    {
        checkComboBox.SelectedItems.Clear();
        if (viewModelCollection != null)
        {
            foreach (var item in viewModelCollection)
            {
                checkComboBox.SelectedItems.Add(item);
            }
        }
    }
}
