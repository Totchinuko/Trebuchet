using System;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Messages;

namespace Trebuchet.Panels
{
    public abstract class Panel : MenuElement,
        ICommand,
        INotifyPropertyChanged,
        IRecipient<PanelRefreshConfigMessage>
    {
        
        private bool _active;

        public Panel(string template) : base(template, "TabButtonTemplate")
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
                OnPropertyChanged(nameof(TabClass));
            }
        }

        public Bitmap? Icon => string.IsNullOrEmpty(IconPath) ? null : TrebuchetUtils.Utils.LoadFromResource(new Uri(IconPath, UriKind.Absolute));
        public string IconPath { get; set; } = string.Empty;
        public string TabClass => Active ? "AppTabBlue" : "AppTabNeutral";

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

        public virtual void PanelDisplayed()
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