using System;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace TrebuchetUtils.Modals
{
    public class QuestionModal : BaseModal
    {
        public QuestionModal(string title, string message) : base(550,220,"Information","QuestionModal")
        {
            CancelCommand = ReactiveCommand.Create(OnCancelModal);
            YesCommand = ReactiveCommand.Create(OnYesModal);

            Message = message;
            MessageTitle = title;
            CloseDisabled = false;
        }

        public ReactiveCommand<Unit,Unit> CancelCommand { get; private set; }

        public string Message { get; }

        public string MessageTitle { get; }

        public bool Result { get; private set; }


        public ReactiveCommand<Unit,Unit> YesCommand { get; private set; }

        private void OnCancelModal()
        {
            Window.Close();
        }

        private void OnYesModal()
        {
            Result = true;
            Window.Close();
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}