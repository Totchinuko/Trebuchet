using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoogGUI.Controls
{
    /// <summary>
    /// Interaction logic for AppMenu.xaml
    /// </summary>
    public partial class AppMenu : UserControl
    {
        private GButton? _current = null;

        public event EventHandler<IGUIPanel?>? ContentSelected;

        public AppMenu()
        {
            InitializeComponent();
        }

        protected virtual void UpdateCurrent(object sender)
        {
            if (sender is not GButton button) return;
            if (_current != null)
                _current.ButtonAccent = false;
            _current = button;
            _current.ButtonAccent = true;
        }

        protected virtual void OnContentSelected(IGUIPanel? panel)
        {
            ContentSelected?.Invoke(this, panel);
        }

        private void Modlist_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateCurrent(sender);
            OnContentSelected(null);
        }

        private void Game_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateCurrent(sender);
            OnContentSelected(null);
        }

        private void Server_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateCurrent(sender);
            OnContentSelected(null);
        }

        private void Settings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateCurrent(sender);
            OnContentSelected(new SettingsContent());
        }
    }
}
