using GoogGUI.Controls;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ICommand _tabCommand;
        private GButton _currentTab;
        public MainWindow()
        {
            InitializeComponent();

            TabCommand = new SimpleCommand(OnTabClicked);

            DataContext = this;
        }

        public ICommand TabCommand { get => _tabCommand; set => _tabCommand = value; }
        private void OnTabClicked(object? obj)
        {
            if (obj == null || obj is not GButton gbutton)
                return;

            if (_currentTab != null)
                _currentTab.ButtonAccent = false;
            _currentTab = gbutton;
            if (_currentTab != null)
                _currentTab.ButtonAccent = true;
        }

        private void Settings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ContentPanelPresenter.Content = new SettingsContent();
        }
    }
}