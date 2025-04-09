using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Trebuchet.ViewModels;
using TrebuchetUtils;

namespace Trebuchet.Windows
{
    /// <summary>
    /// Interaction logic for WorkshopSearch.xaml
    /// </summary>
    public partial class WorkshopSearch : WindowAutoPadding
    {
        public WorkshopSearch()
        {
            InitializeComponent();
            DataContextProperty.Changed.AddClassHandler<WorkshopSearch>(WorkshopSearchContextChanged);
        }

        private void WorkshopSearchContextChanged(WorkshopSearch sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.OldValue is WorkshopSearchViewModel vm)
                vm.PageLoaded -= OnPageLoaded;
            if (args.NewValue is WorkshopSearchViewModel nvm)
                nvm.PageLoaded += OnPageLoaded;
        }

        private void OnPageLoaded(object? sender, EventArgs e)
        {
            var scrollViewer = this.FindControl<ScrollViewer>(@"PageScrollViewer");
            scrollViewer?.ScrollToHome();
        }
    }
}