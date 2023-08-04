using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class ErrorModal : BaseModal
    {
        private string _errorMessage = string.Empty;
        private string _errorTitle = string.Empty;
        private bool _exitApp = false;

        public ErrorModal(string title, string message, bool exitApp = false)
        {
            CloseCommand = new SimpleCommand(OnCloseModal);

            _errorMessage = message;
            _errorTitle = title;
            _exitApp = exitApp;
        }

        public ICommand CloseCommand { get; private set; }
        public string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }
        public string ErrorTitle { get => _errorTitle; set => _errorTitle = value; }
        public override int ModalHeight => 200;

        public override string ModalTitle => "Error";

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ErrorModal"];

        public override int ModalWidth => 650;

        public override void OnWindowClose()
        {
            if (_exitApp)
                Application.Current.Shutdown();
        }

        private void OnCloseModal(object? obj)
        {
            if (_exitApp)
                Application.Current.Shutdown();
            _windows?.Close();
        }
    }
}