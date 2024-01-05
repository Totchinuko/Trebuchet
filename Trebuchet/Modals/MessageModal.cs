using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Trebuchet
{
    public class MessageModal : BaseModal
    {
        private int _height = 200;
        private string _message = string.Empty;
        private string _messageTitle = string.Empty;

        public MessageModal(string title, string message, int height = 200) : base()
        {
            CloseCommand = new SimpleCommand(OnCloseModal);

            _message = message;
            _messageTitle = title;
            _height = height;
            _window.Height = height;
        }

        public ICommand CloseCommand { get; private set; }

        public string Message { get => _message; set => _message = value; }

        public string MessageTitle { get => _messageTitle; set => _messageTitle = value; }

        public override int ModalHeight => _height;

        public override string ModalTitle => "Information";

        public override int ModalWidth => 650;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["MessageModal"];

        public override void OnWindowClose()
        {
        }

        private void OnCloseModal(object? obj)
        {
            _window?.Close();
        }
    }
}