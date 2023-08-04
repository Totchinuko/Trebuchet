using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class ExceptionModal : BaseModal
    {
        private string _errorMessage = string.Empty;
        private string _errorTitle = string.Empty;
        private bool _exitApp = false;

        public ExceptionModal(Exception exception)
        {
            CloseCommand = new SimpleCommand(OnCloseModal);
            _errorTitle = "Internal Exception";
            _errorMessage = exception.GetAllExceptions();
            Clipboard.SetText(_errorMessage);
        }

        public ICommand CloseCommand { get; private set; }
        public string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }
        public string ErrorTitle { get => _errorTitle; set => _errorTitle = value; }
        public override int ModalHeight => 400;

        public override string ModalTitle => "Exception";

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ExceptionModal"];

        public override int ModalWidth => 650;

        public override void OnWindowClose()
        {
            Application.Current.Shutdown();
        }

        private void OnCloseModal(object? obj)
        {
            _windows?.Close();
            Application.Current.Shutdown();
        }
    }
}