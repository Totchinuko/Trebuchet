using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GoogGUI.Controls
{
    /// <summary>
    /// Interaction logic for MapList.xaml
    /// </summary>
    public partial class MapList : UserControl
    {
        public static readonly DependencyProperty SelectedMapProperty = DependencyProperty.Register("SelectedMap", typeof(string), typeof(MapList), new PropertyMetadata(string.Empty));

        public Dictionary<string, string> MapListData { get; set; }

        public SimpleCommand MapSelectCommand { get; private set; }

        public string SelectedMap
        {
            get => (string)GetValue(SelectedMapProperty);
            set => SetValue(SelectedMapProperty, value);
        }

        public MapList()
        {
            MapListData = ServerProfile.GetMapList();
            MapSelectCommand = new SimpleCommand(OnMapSelect);
            InitializeComponent();
        }

        private void OnMapSelect(object? obj)
        {
            if (obj is not string mapPath) return;
            SelectedMap = mapPath;
            MapListPopup.IsOpen = false;
        }

        private void MapList_Click(object sender, RoutedEventArgs e)
        {
            MapListPopup.IsOpen = !MapListPopup.IsOpen;
        }
    }
}