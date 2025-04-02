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
using Trebuchet.Assets;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.SettingFields
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
    public abstract class Field : BaseViewModel
    {
        private readonly string _template;
        public bool DisplayPanel { get; }

        protected Field(string template, bool displayPanel)
        {
            _template = template;
            DisplayPanel = displayPanel;
            HyperlinkCommand = new SimpleCommand().Subscribe(OnHyperlinkClicked);
            ResetCommand = new SimpleCommand().Subscribe(OnReset);
        }

        private static readonly JsonSerializerOptions Options = new();
        
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
        public event EventHandler<Field>? ValueChanged;

        public static List<Field> BuildFieldList(string json, object target, PropertyInfo? property = null)
        {
            List<Field>? fields = JsonSerializer.Deserialize<List<Field>>(json, Options);
            if (fields == null) throw new Exception("Could not deserialize json fields definition.");

            ApplyTranslation(fields);
            foreach (var field in fields)
                field.SetTarget(target, property);
            return fields;
        }

        public abstract void RefreshValue();

        public abstract void RefreshVisibility();

        public abstract void ResetToDefault();

        public abstract void SetTarget(object target, PropertyInfo? property = null);

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

        private static void ApplyTranslation(List<Field> fields)
        {
            foreach (var field in fields)
            {
                field.Name = ApplyTranslation(field.Name);
                field.Description = ApplyTranslation(field.Description);
            }
        }

        private static string ApplyTranslation(string content)
        {
            if(!content.StartsWith("resx:")) return content;
            content = content.Substring(5);
            var property = typeof(Resources).GetProperty(content, BindingFlags.Static | BindingFlags.Public);
            if (property == null) return $"Invalid Property {content}";
            if (property.PropertyType != typeof(string)) return $"Invalid Property {content}";
            return property.GetValue(null, null) as string ?? $"Invalid Property {content}";
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


        public virtual T? Value
        {
            get
            {
                if (_propertyInfos == null) throw new Exception($"Missing property information for {Property}.");
                return GetConvert(_propertyInfos.GetValue(GetTarget()));
            }
            set => SetValue(value);
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

        private void SetValue(T? value)
        {
            if (_propertyInfos == null) throw new Exception($"Missing property information for {Property}.");
            RemoveCollectionEvent();
            _propertyInfos.SetValue(GetTarget(), SetConvert(value));
            AddCollectionEvent();
            OnValueChanged();
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(IsDefault));
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
    }
}