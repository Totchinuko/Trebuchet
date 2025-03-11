using System;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace TrebuchetUtils.Modals
{
    public class QuestionModal : BaseModal
    {
        private string _message = string.Empty;
        private string _messageTitle = string.Empty;
        private bool _result = false;

        public QuestionModal(string title, string message) : base()
        {
            CancelCommand = new SimpleCommand(OnCancelModal);
            YesCommand = new SimpleCommand(OnYesModal);

            _message = message;
            _messageTitle = title;
        }

        public ICommand CancelCommand { get; private set; }

        public override bool CloseDisabled => false;
        public string Message { get => _message; set => _message = value; }

        public string MessageTitle { get => _messageTitle; set => _messageTitle = value; }

        protected override int ModalHeight => 200;

        public override string ModalTitle => "Information";

        protected override int ModalWidth => 550;
        public bool Result { get => _result; set => _result = value; }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["QuestionModal"];

        public ICommand YesCommand { get; private set; }

        public override void OnWindowClose()
        {
        }

        private void OnCancelModal(object? obj)
        {
            _window.Close();
        }

        private void OnYesModal(object? obj)
        {
            _result = true;
            _window.Close();
        }

        public override void Submit()
        {
            YesCommand?.Execute(this);
        }

        public override void Cancel()
        {
            CancelCommand?.Execute(this);
        }
    }
}