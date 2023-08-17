using GoogGUI.SettingFields;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    [JsonDerivedType(typeof(IntField), "Int")]
    [JsonDerivedType(typeof(ToggleField), "Toggle")]
    [JsonDerivedType(typeof(IntSliderField), "IntSlider")]
    [JsonDerivedType(typeof(TextField), "Text")]
    [JsonDerivedType(typeof(TextListField), "TextList")]
    [JsonDerivedType(typeof(DirectoryField), "Directory")]
    [JsonDerivedType(typeof(MapField), "Map")]
    [JsonDerivedType(typeof(CPUAffinityField), "CPUAffinity")]
    public abstract class Field : INotifyPropertyChanged
    {
        private static JsonSerializerOptions _options = new JsonSerializerOptions();

        private string _description = string.Empty;
        private string _hyperlink = string.Empty;
        private string _name = string.Empty;
        private string _property = string.Empty;
        private bool _refreshApp = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<Field>? ValueChanged;

        public string Description { get => _description; set => _description = value; }

        public virtual bool DisplayGenericDescription => true;

        public string Hyperlink { get => _hyperlink; set => _hyperlink = value; }

        public string Name { get => _name; set => _name = value; }

        public string Property { get => _property; set => _property = value; }

        public bool RefreshApp { get => _refreshApp; set => _refreshApp = value; }

        public static List<Field> BuildFieldList(string json, object target, PropertyInfo? property = null)
        {
            List<Field>? fields = JsonSerializer.Deserialize<List<Field>>(json, _options);
            if (fields == null) throw new Exception("Could not deserialize json fields definition.");

            foreach (var field in fields)
                field.SetTarget(target, property);
            return fields;
        }

        public abstract void RefreshValue();

        public abstract void SetTarget(object target, PropertyInfo? property = null);

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this, this);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public abstract class Field<T, D> : Field, ITemplateHolder
    {
        private D? _default = default;
        private PropertyInfo? _propertyInfos;
        private object? _target;
        private PropertyInfo? _targetProperty;

        public Field()
        {
            ResetCommand = new SimpleCommand(OnReset);
            HyperlinkCommand = new SimpleCommand(OnHyperlinkClicked);
        }

        public D? Default { get => _default; set => _default = value; }

        public ICommand HyperlinkCommand { get; private set; }

        public abstract bool IsDefault { get; }

        public ICommand ResetCommand { get; private set; }

        public abstract DataTemplate Template { get; }

        public T? Value
        {
            get
            {
                if (_propertyInfos == null) throw new System.Exception($"Missing property information for {Property}.");
                return GetConvert(_propertyInfos.GetValue(GetTarget()));
            }
            set
            {
                if (_propertyInfos == null) throw new System.Exception($"Missing property information for {Property}.");
                RemoveCollectionEvent();
                _propertyInfos.SetValue(GetTarget(), SetConvert(value));
                AddCollectionEvent();
                OnPropertyChanged("Value");
                OnPropertyChanged("IsDefault");
            }
        }

        public override void RefreshValue()
        {
            OnPropertyChanged("Value");
            OnPropertyChanged("IsDefault");
        }

        public override void SetTarget(object target, PropertyInfo? property = null)
        {
            _targetProperty = property;
            _target = target;
            _propertyInfos = GetTarget().GetType().GetProperty(Property);
            AddCollectionEvent();
        }

        protected abstract T? GetConvert(object? value);

        protected abstract void ResetToDefault();

        protected abstract object? SetConvert(T? value);

        private void AddCollectionEvent()
        {
            if (Value is INotifyCollectionChanged collection)
                collection.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Value");
            OnPropertyChanged("IsDefault");
        }

        private void OnHyperlinkClicked(object? obj)
        {
            using(Process process = new Process())
            {
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = Hyperlink;
                process.Start();
            }
        }

        private object GetTarget()
        {
            if (_targetProperty == null)
                return _target ?? throw new NullReferenceException("Target is not set to a value.");
            else
                return _targetProperty.GetValue(_target) ?? throw new NullReferenceException("Target is not set to a value.");
        }

        private void OnReset(object? obj)
        {
            ResetToDefault();
        }

        private void RemoveCollectionEvent()
        {
            if (Value is INotifyCollectionChanged collection)
                collection.CollectionChanged -= OnCollectionChanged;
        }
    }
}