using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
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

        public ExceptionModal(Exception exception) : base()
        {
            CloseCommand = new SimpleCommand(OnCloseModal);
            _errorTitle = "Internal Exception";
            _errorMessage = exception.GetAllExceptions();
            Clipboard.SetText(_errorMessage);
            if (!((App)Application.Current).IsShutingDown)
                SystemSounds.Exclamation.Play();
        }

        public ExceptionModal(AggregateException exceptions) : base()
        {
            CloseCommand = new SimpleCommand(OnCloseModal);
            _errorTitle = "Internal Exception";
            _errorMessage = exceptions.GetAllExceptions();
            Clipboard.SetText(_errorMessage);
            if (!((App)Application.Current).IsShutingDown)
                SystemSounds.Exclamation.Play();
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
            Application.Current.Shutdown();
            _window?.Close();
        }
    }
}