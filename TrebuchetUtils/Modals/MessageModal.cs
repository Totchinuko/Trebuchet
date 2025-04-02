using System;
using System.Windows;
using System.Windows.Input;

namespace TrebuchetUtils.Modals
{
    public class MessageModal : BaseModal
    {
        public MessageModal(string title, string message, int height = 200) : base(650, height, "Information", "MessageModal")
        {
            CloseCommand = new SimpleCommand().Subscribe(OnCloseModal);

            Message = message;
            MessageTitle = title;
        }

        public ICommand CloseCommand { get; private set; }

        public string Message {get;}

        public string MessageTitle {get;}

        private void OnCloseModal(object? obj)
        {
            Window?.Close();
        }

        public override void Submit()
        {
            CloseCommand.Execute(this);
        }

        public override void Cancel()
        {
            CloseCommand.Execute(this);
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}