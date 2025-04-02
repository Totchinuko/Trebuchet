using System;
using System.Windows.Input;

namespace TrebuchetUtils.Modals
{
    public class QuestionModal : BaseModal
    {
        public QuestionModal(string title, string message) : base(550,220,"Information","QuestionModal")
        {
            CancelCommand = new SimpleCommand().Subscribe(OnCancelModal);
            YesCommand = new SimpleCommand().Subscribe(OnYesModal);

            Message = message;
            MessageTitle = title;
            CloseDisabled = false;
        }

        public ICommand CancelCommand { get; private set; }

        public string Message { get; }

        public string MessageTitle { get; }

        public bool Result { get; private set; }


        public ICommand YesCommand { get; private set; }

        private void OnCancelModal(object? obj)
        {
            Window.Close();
        }

        private void OnYesModal(object? obj)
        {
            Result = true;
            Window.Close();
        }

        public override void Submit()
        {
            YesCommand.Execute(this);
        }

        public override void Cancel()
        {
            CancelCommand.Execute(this);
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}