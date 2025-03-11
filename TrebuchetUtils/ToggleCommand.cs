using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TrebuchetUtils
{
    public class ToggleCommand : ICommand, INotifyPropertyChanged
    {
        private bool _enabled = true;
        private bool _toggled = false;
        private Style _offStyle;
        private Style _onStyle;

        public event EventHandler? CanExecuteChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Action<object?, bool> _execute;

        public Style Style => _toggled ? _onStyle : _offStyle;

        public static Style GetStyle(string key)
        {
            return (Style)Application.Current.Resources[key];
        }

        public ToggleCommand(Action<object?, bool> execute, bool defaultState, Style offStyle, Style onStyle, bool enabled = true)
        {
            _toggled = defaultState;
            _offStyle = offStyle;
            _onStyle = onStyle;
            _execute = execute;
            _enabled = enabled;
        }

        public bool CanExecute(object? parameter)
        {
            return _enabled;
        }

        public void Execute(object? parameter)
        {
            if (_enabled)
            {
                _toggled = !_toggled;
                _execute(parameter, _toggled);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Style)));
            }
        }

        public void SetToggle(bool value)
        {
            if(_toggled == value) return;
            _toggled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Style)));
        }

        public void IsEnabled(bool enabled)
        {
            _enabled = enabled;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
