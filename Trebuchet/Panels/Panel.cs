using CommunityToolkit.Mvvm.Messaging;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Trebuchet.Messages;

namespace Trebuchet
{
    public abstract class Panel : MenuElement,
        ICommand,
        INotifyPropertyChanged,
        ITemplateHolder,
        IRecipient<PanelRefreshConfigMessage>
    {
        protected bool _active;

        public Panel()
        {
            StrongReferenceMessenger.Default.RegisterAll(this);
        }

        public event EventHandler? CanExecuteChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                OnPropertyChanged(nameof(Active));
            }
        }

        public ImageSource? Icon => string.IsNullOrEmpty(IconPath) ? null : new BitmapImage(new Uri(IconPath, UriKind.Relative));

        public string IconPath { get; set; } = string.Empty;

        public abstract DataTemplate Template { get; }

        public virtual bool CanExecute(object? parameter)
        {
            return true;
        }

        public virtual void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                StrongReferenceMessenger.Default.Send(new PanelActivateMessage(this));
        }

        public void Receive(PanelRefreshConfigMessage message)
        {
            RefreshPanel();
        }

        public virtual void RefreshPanel()
        {
        }

        protected void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}