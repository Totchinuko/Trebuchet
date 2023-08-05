using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace GoogGUI
{
    public class Field<T> : INotifyPropertyChanged, IField
    {
        private Action<string, object?>? _callback;
        private string _fieldName = string.Empty;
        private Func<T?>? _getDefault;
        private Func<T?, bool>? _isDefault;
        private string _property = string.Empty;
        private object _template;
        private T? _value;

        public Field(string name, string property, T? value, string template)
        {
            ResetCommand = new SimpleCommand(OnReset);

            _fieldName = name;
            _property = property;
            _template = Application.Current.Resources[template];
            _value = value;

            if (string.IsNullOrEmpty(_property))
                throw new ArgumentException($"Missing property for {_fieldName}");

            if (string.IsNullOrEmpty(template))
                throw new ArgumentException($"Missing template for {_fieldName}");

            if (_template == null)
                throw new Exception($"Template {template} not found");

            if (_value is INotifyCollectionChanged collection)
                collection.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnValueChanged(_value);
            OnPropertyChanged("Value");
            OnPropertyChanged("IsDefault");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string FieldName { get => _fieldName; set => _fieldName = value; }

        public bool IsDefault => _isDefault?.Invoke(_value) ?? true;

        public string Property { get => _property; private set => _property = value; }

        public ICommand ResetCommand { get; private set; }

        public object Template { get => _template; set => _template = value; }

        public object? Value
        {
            get => _value;
            set
            {
                _value = (T?)value;
                OnValueChanged(_value);
                OnPropertyChanged("Value");
                OnPropertyChanged("IsDefault");
            }
        }

        public static bool IsNullOrEmpty(object? value)
        {
            if (value == null) return true;
            if (value is not string svalue) return false;
            return string.IsNullOrEmpty(svalue);
        }

        public Field<T> WhenChanged(Action<string, object?> callback)
        {
            _callback = callback;
            return this;
        }

        public Field<T> WithDefault(Func<T?, bool> isDefault, Func<T?> getDefault)
        {
            _isDefault = isDefault;
            _getDefault = getDefault;
            return this;
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnValueChanged(T? value)
        {
            _callback?.Invoke(Property, value);
        }

        private void OnReset(object? obj)
        {
            if (_getDefault != null)
                Value = _getDefault.Invoke();
        }
    }
}