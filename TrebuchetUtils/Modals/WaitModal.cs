using System;
using System.ComponentModel;
using System.Windows.Input;

namespace TrebuchetUtils.Modals
{
    public class WaitModal : BaseModal, INotifyPropertyChanged
    {
        private readonly Action _cancelCallback;
        private string _message;
        private string _messageTitle;

        public WaitModal(string title, string message, Action cancelCallback) : base(650,200, "Please wait...", "WaitModal")
        {
            CloseCommand = new SimpleCommand(OnCloseModal);
            _message = message;
            _messageTitle = title;
            _cancelCallback = cancelCallback;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand CloseCommand { get; private set; }

        public string Message 
        { 
            get => _message; 
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            } 
        }

        public string MessageTitle 
        { 
            get => _messageTitle; 
            set
            {
                _messageTitle = value;
                OnPropertyChanged(nameof(MessageTitle));
            }
        }
        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnCloseModal(object? obj)
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