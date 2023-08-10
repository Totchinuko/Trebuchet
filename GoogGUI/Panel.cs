using Goog;
using GoogGUI.Attributes;
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
        protected string _icon;
        protected string _label;
        protected string _template;
        protected Config _config;

        public Panel(Config config)
        {
            _config = config;
            PanelAttribute attr = GetType().GetCustomAttribute<PanelAttribute>() ?? throw new Exception($"Panel {GetType()} is missing an attribute.");
            _label = attr.Label;
            _icon = attr.Icon;
            _template = attr.Template;
        }

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

        public virtual ImageSource Icon => new BitmapImage(new Uri(_icon, UriKind.Relative));

        public virtual string Label => _label;

        public virtual DataTemplate Template => (DataTemplate)Application.Current.Resources[_template];

        public virtual bool CanExecute(object? parameter)
        {
            return true;
        }

        public virtual void Execute(object? parameter)
        {
            if(CanExecute(parameter))
                ((MainWindow)Application.Current.MainWindow).App.Panel = this;
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}