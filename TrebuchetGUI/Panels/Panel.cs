using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Trebuchet
{
    public abstract class Panel : MenuElement, ICommand, INotifyPropertyChanged, ITemplateHolder
    {
        protected bool _active;
        protected Config _config;
        protected UIConfig _uiConfig;

        public Panel(Config config, UIConfig uiConfig)
        {
            _config = config;
            _uiConfig = uiConfig;
        }

        public event EventHandler? AppConfigurationChanged;

        public event EventHandler? CanExecuteChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                OnPropertyChanged("Active");
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
                ((MainWindow)Application.Current.MainWindow).App.ActivePanel = this;
        }

        public virtual void RefreshPanel()
        {
        }

        protected void OnAppConfigurationChanged()
        {
            AppConfigurationChanged?.Invoke(this, EventArgs.Empty);
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