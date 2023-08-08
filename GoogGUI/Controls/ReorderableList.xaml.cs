using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogGUI.Controls
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

            if (removedIdx < targetIdx)
            {
                ItemsSource.Insert(targetIdx + 1, droppedData);
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
            OnPropertyChanged("ItemsSource");
        }

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border dragged)
            {
                e.Handled = true;
                _draggedObject = dragged;
                DragDrop.DoDragDrop(dragged, dragged.DataContext, DragDropEffects.Move);
            }
        }

        private void OnRemoved(object? obj)
        {
            ItemsSource.Remove(obj);
        }
    }
}