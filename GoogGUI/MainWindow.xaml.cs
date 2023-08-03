using GoogGUI.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppMenu.ContentSelected += OnContentSelected;
            DataContext = this;
        }

        private void OnContentSelected(object? sender, UserControl? e)
        {
            ContentPanelPresenter.Content = e;
        }
    }
}