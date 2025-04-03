﻿using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
            MapListData = Tools.GetMapList();
            MapSelectCommand.Subscribe(OnMapSelect);
            InitializeComponent();
        }
        
        public Dictionary<string, string> MapListData { get; set; }

        public SimpleCommand<string> MapSelectCommand { get; private set; } = new();
        
        public string SelectedMap
        {
            get => (string)GetValue(SelectedMapProperty);
            set => SetValue(SelectedMapProperty, value);
        }
        
        private void MapList_Click(object sender, RoutedEventArgs e)
        {
            MapListPopup.IsOpen = !MapListPopup.IsOpen;
        }
        
        private void OnMapSelect(string? mapPath)
        {
            if (mapPath is null) return;
            SelectedMap = mapPath;
            MapListPopup.IsOpen = false;
        }
    }
}