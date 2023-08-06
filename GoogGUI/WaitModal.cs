using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class WaitModal : BaseModal
    {
        private string _message = string.Empty;
        private string _messageTitle = string.Empty;
        private Action _cancelCallback;

        public WaitModal(string title, string message, Action cancelCallback) : base()
        {
            CloseCommand = new SimpleCommand(OnCloseModal);
            _message = message;
            _messageTitle = title;
            _cancelCallback = cancelCallback;
        }

        public ICommand CloseCommand { get; private set; }
        public string Message { get => _message; set => _message = value; }
        public string MessageTitle { get => _messageTitle; set => _messageTitle = value; }
        public override int ModalHeight => 200;

        public override string ModalTitle => "Working";

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["MessageModal"];

        public override int ModalWidth => 650;

        public override void OnWindowClose()
        {
        }

        private void OnCloseModal(object? obj)
        {
            _cancelCallback.Invoke();
            _window?.Close();
        }
    }
}