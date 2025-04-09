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
        }
        
        public WorkshopSearchViewModel? SearchViewModel { get; set; }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            SearchViewModel?.OnSearch();
        }
    }
}