using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class Field<T> : INotifyPropertyChanged, IField
    {
        private string _fieldName = string.Empty;
        private Func<T?>? _getDefault;
        private Func<T?, bool>? _isDefault;
        private PropertyInfo _property;
        private object _target;
        private object _template;

        public Field(string name, PropertyInfo property, object target, string template)
        {
            ResetCommand = new SimpleCommand(OnReset);

            _fieldName = name;
            _property = property;
            _template = Application.Current.Resources[template];
            _target = target;

            if (string.IsNullOrEmpty(template))
                throw new ArgumentException($"Missing template for {_fieldName}");

            if (_template == null)
                throw new Exception($"Template {template} not found");

            if (Value is INotifyCollectionChanged collection)
                collection.CollectionChanged += OnCollectionChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string FieldName { get => _fieldName; set => _fieldName = value; }

        public bool IsDefault => _isDefault?.Invoke((T?)Value) ?? true;

        public ICommand ResetCommand { get; private set; }

        public object Template { get => _template; set => _template = value; }

        public object? Value
        {
            get => (T?)_property.GetValue(_target);
            set
            {
                _property.SetValue(_target, (T?)value);
                OnPropertyChanged("Value");
                OnPropertyChanged("IsDefault");
            }
        }

        public void RefreshValue()
        {
            OnPropertyChanged("Value");
        }

        public static bool IsNullOrEmpty(object? value)
        {
            if (value == null) return true;
            if (value is not string svalue) return false;
            return string.IsNullOrEmpty(svalue);
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

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Value");
            OnPropertyChanged("IsDefault");
        }

        private void OnReset(object? obj)
        {
            if (_getDefault != null)
                Value = _getDefault.Invoke();
        }
    }
}