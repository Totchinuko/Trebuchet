using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for ReorderableList.xaml
    /// </summary>
    public partial class ReorderableList : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(IList),
            typeof(ReorderableList),
            new PropertyMetadata(
                default(IList)
                )
            );

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
                    "ItemTemplate",
            typeof(DataTemplate),
            typeof(ReorderableList),
            new PropertyMetadata(
                default(DataTemplate)
                )
            );

        public static readonly DependencyProperty ParentElementProperty = DependencyProperty.Register(
                "ParentElement",
                typeof(object),
                typeof(ReorderableList),
                new PropertyMetadata(
                    default(object)
                    )
            );

        private DependencyObject? _draggedObject;

        public ReorderableList()
        {
            RemoveCommand = new SimpleCommand(OnRemoved);
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public IList ItemsSource
        {
            get => (IList)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public object ParentElement
        {
            get => GetValue(ParentElementProperty);
            set => SetValue(ParentElementProperty, value);
        }

        public ICommand RemoveCommand { get; private set; }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Item_DragEnter(object sender, DragEventArgs e)
        {
            if (sender != _draggedObject)
                GuiExtensions.SetIsDraggedOver((DependencyObject)sender, true);
        }

        private void Item_DragLeave(object sender, DragEventArgs e)
        {
            if (sender != _draggedObject)
                GuiExtensions.SetIsDraggedOver((DependencyObject)sender, false);
        }

        private void Item_Drop(object sender, DragEventArgs e)
        {
            GuiExtensions.SetIsDraggedOver((DependencyObject)sender, false);

            var myElement = e.Data.GetFormats();
            object droppedData = e.Data.GetData(myElement[0]);

            object target = ((Border)sender).DataContext;

            int removedIdx = ItemsSource.IndexOf(droppedData);
            int targetIdx = ItemsSource.IndexOf(target);

            if (removedIdx == targetIdx) return;

            if (removedIdx < targetIdx)
            {
                ItemsSource.Insert(targetIdx, droppedData);
                ItemsSource.RemoveAt(removedIdx);
            }
            else
            {
                int remIdx = removedIdx + 1;
                if (ItemsSource.Count + 1 > remIdx)
                {
                    ItemsSource.Insert(targetIdx, droppedData);
                    ItemsSource.RemoveAt(remIdx);
                }
            }
            OnPropertyChanged(nameof(ItemsSource));
        }

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border dragged)
            {
                e.Handled = true;
                _draggedObject = dragged;
                GuiExtensions.SetIsDragging(dragged, true);
                DragDrop.DoDragDrop(dragged, dragged.DataContext, DragDropEffects.Move);
                GuiExtensions.SetIsDragging(dragged, false);
            }
        }

        private void OnRemoved(object? obj)
        {
            ItemsSource.Remove(obj);
        }

        private void ScrollArea_DragOver(object sender, DragEventArgs e)
        {
            ScrollViewer container = (ScrollViewer)sender;

            if (container == null) return;

            double tolerance = 60;
            double verticalPos = e.GetPosition(container).Y;
            double offset = 20;

            if (verticalPos < tolerance) // Top of visible list?
            {
                //Scroll up
                container.ScrollToVerticalOffset(container.VerticalOffset - offset);
            }
            else if (verticalPos > container.ActualHeight - tolerance) //Bottom of visible list?
            {
                //Scroll down
                container.ScrollToVerticalOffset(container.VerticalOffset + offset);
            }
        }
    }
}