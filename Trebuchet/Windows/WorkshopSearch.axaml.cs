using CommunityToolkit.Mvvm.Messaging;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using SteamWorksWebAPI.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Trebuchet.Messages;
using Trebuchet.ViewModels;
using TrebuchetLib;
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