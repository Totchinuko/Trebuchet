﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class WaitModal : BaseModal, INotifyPropertyChanged
    {
        private Action _cancelCallback;
        private string _message = string.Empty;
        private string _messageTitle = string.Empty;

        public WaitModal(string title, string message, Action cancelCallback) : base()
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
                OnPropertyChanged("Message");
            } 
        }

        public string MessageTitle 
        { 
            get => _messageTitle; 
            set
            {
                _messageTitle = value;
                OnPropertyChanged("MessageTitle");
            }
        }

        public override int ModalHeight => 200;

        public override string ModalTitle => "Working";

        public override int ModalWidth => 650;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["WaitModal"];

        public override void OnWindowClose()
        {
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnCloseModal(object? obj)
        {
            _cancelCallback.Invoke();
            _message = "Canceling...";
            OnPropertyChanged("Message");
        }
    }
}