using System;
using System.Reactive;
using ReactiveUI;

namespace TrebuchetUtils.Modals
{
    public class WaitModal : BaseModal
    {
        private readonly Action _cancelCallback;
        private string _message;
        private string _messageTitle;

        public WaitModal(string title, string message, Action cancelCallback) : base(650,200, "Please wait...", "WaitModal")
        {
            CloseCommand = ReactiveCommand.Create(OnCloseModal);
            _message = message;
            _messageTitle = title;
            _cancelCallback = cancelCallback;
        }


        public ReactiveCommand<Unit, Unit> CloseCommand { get; private set; }

        public string Message 
        { 
            get => _message; 
            set => SetField(ref _message, value);
        }

        public string MessageTitle 
        { 
            get => _messageTitle; 
            set => SetField(ref _messageTitle, value);
        }

        private void OnCloseModal()
        {
            _cancelCallback.Invoke();
            _message = "Canceling...";
            OnPropertyChanged(nameof(Message));
        }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }
    }
}