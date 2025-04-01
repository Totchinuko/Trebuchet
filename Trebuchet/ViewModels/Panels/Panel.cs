using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Trebuchet.Messages;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.Panels
{
    public abstract class Panel : MenuElement,
        ICommand,
        INotifyPropertyChanged,
        ITinyRecipient<PanelRefreshConfigMessage>
    {
        
        private bool _active;

        public Panel(string label, string iconPath, bool bottom) : base(label)
        {
            IconPath = iconPath;
            BottomPosition = bottom;
            TinyMessengerHub.Default.Subscribe(this);
        }

        public event EventHandler? CanExecuteChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public string IconPath { get; }
        public bool BottomPosition { get; }

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

        public string TabClass => Active ? "AppTabBlue" : "AppTabNeutral";

        public virtual bool CanExecute(object? parameter)
        {
            return true;
        }

        public virtual void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                TinyMessengerHub.Default.Publish(new PanelActivateMessage(this, this));
        }

        public void Receive(PanelRefreshConfigMessage message)
        {
            RefreshPanel();
        }

        public virtual void RefreshPanel()
        {
        }

        public virtual Task Tick()
        {
            return Task.CompletedTask;
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