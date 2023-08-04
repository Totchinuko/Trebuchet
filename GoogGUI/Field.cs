using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class Field : INotifyPropertyChanged
    {
        private object? _default;
        private string _fieldName = string.Empty;
        private string _property = string.Empty;
        private object _template;
        private object? _value;

        public Field(string name, string property, object target, object? defaultValue, string template, Action<Field, object?>? callback = null)
        {
            ResetCommand = new SimpleCommand(OnReset);

            _fieldName = name;
            _property = property;
            if (string.IsNullOrEmpty(_property))
                throw new ArgumentException($"Missing property for {_fieldName}");

            if (string.IsNullOrEmpty(template))
                throw new ArgumentException($"Missing template for {_fieldName}");

            PropertyInfo? prop = target.GetType().GetProperty(_property);

            if (prop == null)
                throw new NullReferenceException($"{_property} is not found on type {target.GetType()}");

            _value = prop.GetValue(target);
            _default = defaultValue;
            _template = Application.Current.Resources[template];
            if (_template == null)
                throw new Exception($"Template {template} not found");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private event Action<Field, object?>? _valueChanged;

        public object? Default { get => _default; set => _default = value; }

        public string FieldName { get => _fieldName; set => _fieldName = value; }

        public bool IsDefault => _default?.Equals(_value) ?? _value == null;

        public string Property { get => _property; private set => _property = value; }

        public ICommand ResetCommand { get; private set; }

        public object Template { get => _template; set => _template = value; }

        public object? Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged(_value);
                OnPropertyChanged("Value");
                OnPropertyChanged("IsDefault");
            }
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnValueChanged(object? value)
        {
            _valueChanged?.Invoke(this, value);
        }

        private void OnReset(object? obj)
        {
            Value = _default;
        }
    }
}