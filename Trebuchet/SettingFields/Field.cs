#region

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Templates;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

#endregion

namespace Trebuchet.SettingFields
{
    [JsonDerivedType(typeof(IntField), "Int")]
    [JsonDerivedType(typeof(ToggleField), "Toggle")]
    [JsonDerivedType(typeof(IntSliderField), "IntSlider")]
    [JsonDerivedType(typeof(TextField), "Text")]
    [JsonDerivedType(typeof(TextListField), "TextList")]
    [JsonDerivedType(typeof(DirectoryField), "Directory")]
    [JsonDerivedType(typeof(MapField), "Map")]
    [JsonDerivedType(typeof(CpuAffinityField), "CPUAffinity")]
    [JsonDerivedType(typeof(ComboBoxField), "ComboBox")]
    [JsonDerivedType(typeof(TitleField), "Title")]
    [JsonDerivedType(typeof(RawUdpField), "RawUDPPort")]
    [JsonDerivedType(typeof(FloatField), "FloatField")]
    public abstract class Field : INotifyPropertyChanged
    {
        private readonly string _template;
        public bool DisplayPanel { get; }

        protected Field(string template, bool displayPanel)
        {
            _template = template;
            DisplayPanel = displayPanel;
            HyperlinkCommand = new SimpleCommand(OnHyperlinkClicked);
            ResetCommand = new SimpleCommand(OnReset);
        }

        private static readonly JsonSerializerOptions Options = new();

        public IDataTemplate Template
        {
            get
            {
                if (Application.Current == null) throw new Exception("Application.Current is null");

                if (Application.Current.Resources.TryGetResource(_template, Application.Current.ActualThemeVariant,
                        out var resource) && resource is IDataTemplate template1)
                {
                    return template1;
                }

                throw new Exception($"Template {_template} not found");
            }
        }
        
        public abstract bool IsDefault { get; }

        public virtual bool IsVisible => true;

        public string Description { get; set; } = string.Empty;
        
        public bool DisplayDescription => !string.IsNullOrEmpty(Description) && DisplayGenericDescription;

        public virtual bool DisplayGenericDescription => true;

        public string Hyperlink { get; set; } = string.Empty;

        public ICommand HyperlinkCommand { get; private set; }
        
        public ICommand ResetCommand { get; private set; }

        public bool IsEnabled { get; set; } = true;

        public string Name { get; set; } = string.Empty;

        public string Property { get; set; } = string.Empty;

        public bool RefreshApp { get; set; } = false;

        public virtual bool UseFieldRow => true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<Field>? ValueChanged;

        public static List<Field> BuildFieldList(string json, object target, PropertyInfo? property = null)
        {
            List<Field>? fields = JsonSerializer.Deserialize<List<Field>>(json, Options);
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
        
        private void OnHyperlinkClicked(object? obj)
        {
            using var process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = Hyperlink;
            process.Start();
        }
        
        private void OnReset(object? obj)
        {
            ResetToDefault();
        }
    }

    public abstract class Field<T, TD> : Field
    {
        private PropertyInfo? _propertyInfos;
        private object? _target;
        private PropertyInfo? _targetProperty;

        protected Field(string template, bool displayPanel) : base(template, displayPanel)
        {
        }
        
        protected Field(string template) : base(template, true)
        {
        }

        public FieldCondition? Condition { get; set; } = null;

        public virtual TD? Default { get; set; }


        public override bool IsVisible => Condition == null || Condition.IsVisible(GetTarget());


        public BaseValidation? Validation { get; set; } = null;

        public virtual T? Value
        {
            get
            {
                if (_propertyInfos == null) throw new Exception($"Missing property information for {Property}.");
                return GetConvert(_propertyInfos.GetValue(GetTarget()));
            }
            set
            {
                if (!Validate(value)) return;
                if (_propertyInfos == null) throw new Exception($"Missing property information for {Property}.");
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
            return _targetProperty.GetValue(_target) ??
                   throw new NullReferenceException("Target is not set to a value.");
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(IsDefault));
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
            if (validation.IsValid(value, out var errorMessage)) return true;
            if (string.IsNullOrEmpty(errorMessage)) return false;

            var modal = new ErrorModal("Invalid Value", errorMessage);
            modal.OpenDialogue();
            return false;
        }
    }
}