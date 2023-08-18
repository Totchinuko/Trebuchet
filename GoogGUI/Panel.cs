using Goog;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogGUI
{
    public abstract class Panel : ICommand, INotifyPropertyChanged, ITemplateHolder
    {
        protected bool _active;
        protected Config _config;
        protected UIConfig _uiConfig;

        public Panel(Config config, UIConfig uiConfig)
        {
            _config = config;
            _uiConfig = uiConfig;

            PanelAttribute attr = GetType().GetCustomAttribute<PanelAttribute>() ?? throw new Exception($"Panel {GetType()} is missing an attribute.");
            Label = attr.Label;
            Icon = new BitmapImage(new Uri(attr.Icon, UriKind.Relative));
            Template = (DataTemplate)Application.Current.Resources[attr.Template];
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

        public ImageSource Icon { get; }

        public string Label { get; }

        public virtual DataTemplate Template { get; }

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