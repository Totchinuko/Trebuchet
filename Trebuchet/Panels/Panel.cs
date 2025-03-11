using System;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Messages;
using TrebuchetGUILib;

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
                OnPropertyChanged(nameof(TabStyle));
            }
        }

        public ImageSource? Icon => string.IsNullOrEmpty(IconPath) ? null : new BitmapImage(new Uri(IconPath, UriKind.Relative));
        public string IconPath { get; set; } = string.Empty;
        public Style TabStyle => (Style)(Active ? Application.Current.Resources["TTabLMidBlueBright"] : Application.Current.Resources["TTabLMidNormalStealth"]);
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