using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace GoogGUI
{
    public class QuestionModal : BaseModal
    {
        private string _message = string.Empty;
        private string _messageTitle = string.Empty;
        private DialogResult _result = DialogResult.None;

        public QuestionModal(string title, string message) : base()
        {
            CancelCommand = new SimpleCommand(OnCancelModal);
            YesCommand = new SimpleCommand(OnYesModal);

            _message = message;
            _messageTitle = title;
        }

        public ICommand CancelCommand { get; private set; }

        public string Message { get => _message; set => _message = value; }

        public string MessageTitle { get => _messageTitle; set => _messageTitle = value; }

        public override int ModalHeight => 200;

        public override string ModalTitle => "Information";

        public override int ModalWidth => 550;

        public override bool CloseDisabled => false;

        public DialogResult Result { get => _result; set => _result = value; }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["QuestionModal"];

        public ICommand YesCommand { get; private set; }

        public override void OnWindowClose()
        {
            if (_result == DialogResult.None)
                _result = DialogResult.Cancel;
        }

        private void OnCancelModal(object? obj)
        {
            _result = DialogResult.Cancel;
            _window.Close();
        }

        private void OnYesModal(object? obj)
        {
            _result = DialogResult.Yes;
            _window.Close();
        }
    }
}