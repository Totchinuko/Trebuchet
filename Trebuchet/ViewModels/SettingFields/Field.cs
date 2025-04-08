using System;
using System.Collections.Generic;
using System.Reactive;
using DynamicData.Binding;
using ReactiveUI;

namespace Trebuchet.ViewModels.SettingFields
{
    public interface IValueField
    {
        ReactiveCommand<Unit, Unit> Update { get; }
        ReactiveCommand<Unit, Unit> Reset { get; }
        ReactiveCommand<Unit, Unit> HyperlinkClick { get; }
        bool IsDefault { get; }
        bool HasDefault { get; }
        bool IsVisible { get; }
        string Title { get; }
        string Description { get; }
        bool DisplayGenericDescription { get; }
        bool DisplayDescription { get; }
        bool IsEnabled { get; }
        string Hyperlink { get; }
        
    }
    
    public abstract class FieldElement : ReactiveObject
    {
    }

    public abstract class FieldElement<F> : FieldElement where F : FieldElement<F>
    {
        private string _title = string.Empty;

        public string Title
        {
            get => _title;
            protected set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public F SetTitle(string title)
        {
            Title = title;
            return (F)this;
        }
    }

    public abstract class DescriptiveElement<F> : FieldElement<F> where F : DescriptiveElement<F>
    {
        private string _description = string.Empty;
        private bool _displayDescription;
        private bool _displayGenericDescription = true;

        public string Description
        {
            get => _description;
            protected set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        public bool DisplayDescription
        {
            get => _displayDescription;
            protected set => this.RaiseAndSetIfChanged(ref _displayDescription, value);
        }

        public bool DisplayGenericDescription
        {
            get => _displayGenericDescription;
            protected set => this.RaiseAndSetIfChanged(ref _displayGenericDescription, value);
        }

        public F SetDescription(string description)
        {
            Description = description;
            DisplayDescription = !string.IsNullOrEmpty(Description) && DisplayGenericDescription;
            return (F)this;
        }
    
        public F ToggleGenericDescription(bool toggle)
        {
            DisplayGenericDescription = toggle;
            DisplayDescription = !string.IsNullOrEmpty(Description) && DisplayGenericDescription;
            return (F)this;
        }
    }
    
    public abstract class Field<F> : DescriptiveElement<F> where F : Field<F>
    {
        private bool _isDefault;
        private bool _hasDefault;
        private bool _isVisible = true;
        private string _hyperlink = string.Empty;
        private bool _isEnabled = true;

        public Field()
        {
            HyperlinkClick = ReactiveCommand.Create(() => TrebuchetUtils.Utils.OpenWeb(Hyperlink));
        }

        public bool IsDefault
        {
            get => _isDefault;
            protected set => this.RaiseAndSetIfChanged(ref _isDefault, value);
        }

        public bool HasDefault
        {
            get => _hasDefault;
            protected set => this.RaiseAndSetIfChanged(ref _hasDefault, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public string Hyperlink
        {
            get => _hyperlink;
            protected set => this.RaiseAndSetIfChanged(ref _hyperlink, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            protected set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        public ReactiveCommand<Unit, Unit> HyperlinkClick { get; }
        public abstract ReactiveCommand<Unit, Unit> Reset { get; }
    }
    
    public abstract class Field<F,T> : Field<F>, IValueField where F : Field<F,T>
    {
        private Func<T>? _getter;
        private Action<T>? _setter;
        private Func<T>? _defaultBuilder;
        private T _value;

        protected Field(T initialValue)
        {
            _value = initialValue;
            var canReset = this.WhenAnyValue<Field<F,T>,bool,bool>(x => x.HasDefault, (hasDef) => hasDef);
            
            Reset = ReactiveCommand.Create(
                canExecute: canReset,
                execute: () =>
                {
                    if (DefaultBuilder is not null)
                        Value = DefaultBuilder.Invoke();
                });

            Update = ReactiveCommand.Create(() =>
            {
                if (Getter is not null)
                    Value = Getter.Invoke();
            });
            
            ValueChanged = ReactiveCommand.Create<T>((v) =>
            {
                if (DefaultBuilder != null)
                    IsDefault = IsValueDefault(v, DefaultBuilder());
                else
                    IsDefault = true;
                Setter?.Invoke(v);
            });

            this.WhenAnyValue(x => x.Value)
                .InvokeCommand(ValueChanged);
        }

        public Func<T>? Getter
        {
            get => _getter;
            protected set => this.RaiseAndSetIfChanged(ref _getter, value);
        }

        public Action<T>? Setter
        {
            get => _setter;
            protected set => this.RaiseAndSetIfChanged(ref _setter, value);
        }

        public Func<T>? DefaultBuilder
        {
            get => _defaultBuilder;
            protected set => this.RaiseAndSetIfChanged(ref _defaultBuilder, value);
        }

        public T Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }

        public override ReactiveCommand<Unit, Unit> Reset { get; }
        public ReactiveCommand<T, Unit> ValueChanged { get; }
        public ReactiveCommand<Unit,Unit> Update { get; }

        public F SetDefault(Func<T> defGenerator)
        {
            DefaultBuilder = defGenerator;
            IsDefault = IsValueDefault(_value, DefaultBuilder());
            HasDefault = true;
            return (F)this;
        }

        public F SetSetter(Action<T> setter)
        {
            Setter = setter;
            return (F)this;
        }

        public F SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            return (F)this;
        } 

        public F SetGetter(Func<T> getter)
        {
            Getter = getter;
            _value = Getter.Invoke();
            if(DefaultBuilder is not null)
                IsDefault = IsValueDefault(_value, DefaultBuilder());
            return (F)this;
        }

        public F WhenFieldChanged<P,R>(ReactiveCommand<P,R> command)
        {
            ValueChanged.InvokeCommand(command);
            return (F)this;
        }

        public F UpdateWith<OF, OT>(Field<OF, OT> field) where OF : Field<OF,OT>
        {
            field.ValueChanged.Subscribe((_) => Update.Execute());
            return (F)this;
        }

        protected virtual bool IsValueDefault(T value, T defValue)
        {
            return EqualityComparer<T>.Default.Equals(value, defValue);
        }
    }
}