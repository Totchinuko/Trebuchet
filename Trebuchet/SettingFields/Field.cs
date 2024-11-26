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

using Trebuchet.SettingFields;
using TrebuchetGUILib;

namespace Trebuchet
{
    [JsonDerivedType(typeof(IntField), "Int")]
    [JsonDerivedType(typeof(ToggleField), "Toggle")]
    [JsonDerivedType(typeof(IntSliderField), "IntSlider")]
    [JsonDerivedType(typeof(TextField), "Text")]
    [JsonDerivedType(typeof(TextListField), "TextList")]
    [JsonDerivedType(typeof(DirectoryField), "Directory")]
    [JsonDerivedType(typeof(MapField), "Map")]
    [JsonDerivedType(typeof(CPUAffinityField), "CPUAffinity")]
    [JsonDerivedType(typeof(ComboBoxField), "ComboBox")]
    [JsonDerivedType(typeof(TitleField), "Title")]
    [JsonDerivedType(typeof(RawUDPField), "RawUDPPort")]
    [JsonDerivedType(typeof(FloatField), "FloatField")]
    public abstract class Field : INotifyPropertyChanged
    {
        private static JsonSerializerOptions _options = new JsonSerializerOptions();

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<Field>? ValueChanged;

        public string Description { get; set; } = string.Empty;

        public virtual bool DisplayGenericDescription => true;

        public string Hyperlink { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public string Name { get; set; } = string.Empty;

        public string Property { get; set; } = string.Empty;

        public bool RefreshApp { get; set; } = false;

        public abstract DataTemplate Template { get; }

        public virtual bool UseFieldRow => true;

        public static List<Field> BuildFieldList(string json, object target, PropertyInfo? property = null)
        {
            List<Field>? fields = JsonSerializer.Deserialize<List<Field>>(json, _options);
            if (fields == null) throw new Exception("Could not deserialize json fields definition.");

            foreach (var field in fields)
                field.SetTarget(target, property);
            return fields;
        }

        public abstract void RefreshValue();

        public abstract void RefreshVisibility();

        public abstract void ResetToDefault();

        public abstract void SetTarget(object target, PropertyInfo? property = null);

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this, this);
        }
    }

    public abstract class Field<T, D> : Field, ITemplateHolder
    {
        private PropertyInfo? _propertyInfos;
        private object? _target;
        private PropertyInfo? _targetProperty;

        public Field()
        {
            ResetCommand = new SimpleCommand(OnReset);
            HyperlinkCommand = new SimpleCommand(OnHyperlinkClicked);
        }

        public FieldCondition? Condition { get; set; } = null;

        public virtual D? Default { get; set; } = default;

        public ICommand HyperlinkCommand { get; private set; }

        public abstract bool IsDefault { get; }

        public Visibility IsVisible => Condition == null ? Visibility.Visible : Condition.IsVisible(GetTarget()) ? Visibility.Visible : Visibility.Collapsed;

        public ICommand ResetCommand { get; private set; }

        public BaseValidation? Validation { get; set; } = null;

        public virtual T? Value
        {
            get
            {
                if (_propertyInfos == null) throw new System.Exception($"Missing property information for {Property}.");
                return GetConvert(_propertyInfos.GetValue(GetTarget()));
            }
            set
            {
                if (!Validate(value)) return;
                if (_propertyInfos == null) throw new System.Exception($"Missing property information for {Property}.");
                RemoveCollectionEvent();
                _propertyInfos.SetValue(GetTarget(), SetConvert(value));
                AddCollectionEvent();
                OnValueChanged();
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(IsDefault));
            }
        }

        public override void RefreshValue()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(IsDefault));
        }

        public override void RefreshVisibility()
        {
            OnPropertyChanged(nameof(IsVisible));
        }

        public override void SetTarget(object target, PropertyInfo? property = null)
        {
            _targetProperty = property;
            _target = target;
            _propertyInfos = GetTarget().GetType().GetProperty(Property);
            AddCollectionEvent();
        }

        protected abstract T? GetConvert(object? value);

        protected abstract object? SetConvert(T? value);

        private void AddCollectionEvent()
        {
            if (Value is INotifyCollectionChanged collection)
                collection.CollectionChanged += OnCollectionChanged;
        }

        private object GetTarget()
        {
            if (_targetProperty == null)
                return _target ?? throw new NullReferenceException("Target is not set to a value.");
            else
                return _targetProperty.GetValue(_target) ?? throw new NullReferenceException("Target is not set to a value.");
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(IsDefault));
        }

        private void OnHyperlinkClicked(object? obj)
        {
            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = Hyperlink;
                process.Start();
            }
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

        private bool Validate(T? value)
        {
            if (Validation == null) return true;
            if (Validation is not BaseValidation<T> validation) return false;
            if (validation.IsValid(value, out string errorMessage)) return true;
            if (string.IsNullOrEmpty(errorMessage)) return false;

            ErrorModal modal = new ErrorModal("Invalid Value", errorMessage, false);
            modal.ShowDialog();
            return false;
        }
    }
}