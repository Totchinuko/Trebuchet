using System;
using System.Reactive;
using ReactiveUI;

namespace TrebuchetUtils.Modals
{
    public class MessageModal : BaseModal
    {
        public MessageModal(string title, string message, int height = 200) : base(650, height, "Information", "MessageModal")
        {
            CloseCommand = ReactiveCommand.Create(OnCloseModal);

            Message = message;
            MessageTitle = title;
        }

        public ReactiveCommand<Unit, Unit> CloseCommand { get; private set; }

        public string Message {get;}

        public string MessageTitle {get;}

        private void OnCloseModal()
        {
            Window?.Close();
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}