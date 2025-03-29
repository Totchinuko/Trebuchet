using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Trebuchet.Messages;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for MapList.xaml
    /// </summary>
    public partial class MapList : UserControl
    {
        public static readonly StyledProperty<string> SelectedMapProperty = AvaloniaProperty.Register<MapList, string>(nameof(SelectedMap));

        public MapList()
        {
            MapListData = TinyMessengerHub.Default.InstantReturn(new MapListMessage());
            MapSelectCommand = new SimpleCommand(OnMapSelect);
            InitializeComponent();
        }
        
        public Dictionary<string, string> MapListData { get; set; }
        
        public SimpleCommand MapSelectCommand { get; private set; }
        
        public string SelectedMap
        {
            get => (string)GetValue(SelectedMapProperty);
            set => SetValue(SelectedMapProperty, value);
        }
        
        private void MapList_Click(object sender, RoutedEventArgs e)
        {
            MapListPopup.IsOpen = !MapListPopup.IsOpen;
        }
        
        private void OnMapSelect(object? obj)
        {
            if (obj is not string mapPath) return;
            SelectedMap = mapPath;
            MapListPopup.IsOpen = false;
        }
    }
}