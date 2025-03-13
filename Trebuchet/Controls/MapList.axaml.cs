using System.Collections.Generic;
using Avalonia.Controls;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for MapList.xaml
    /// </summary>
    public partial class MapList : UserControl
    {
        // public static readonly DependencyProperty SelectedMapProperty = DependencyProperty.Register("SelectedMap", typeof(string), typeof(MapList), new PropertyMetadata(string.Empty));

        public MapList()
        {
            // MapListData = ServerProfile.GetMapList();
            // MapSelectCommand = new SimpleCommand(OnMapSelect);
            InitializeComponent();
        }
        //
        // public Dictionary<string, string> MapListData { get; set; }
        //
        // public SimpleCommand MapSelectCommand { get; private set; }
        //
        // public string SelectedMap
        // {
        //     get => (string)GetValue(SelectedMapProperty);
        //     set => SetValue(SelectedMapProperty, value);
        // }
        //
        // private void MapList_Click(object sender, RoutedEventArgs e)
        // {
        //     MapListPopup.IsOpen = !MapListPopup.IsOpen;
        // }
        //
        // private void OnMapSelect(object? obj)
        // {
        //     if (obj is not string mapPath) return;
        //     SelectedMap = mapPath;
        //     MapListPopup.IsOpen = false;
        // }
    }
}