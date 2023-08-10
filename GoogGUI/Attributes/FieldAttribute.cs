using System;
using System.Reflection;

namespace GoogGUI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public abstract class FieldAttribute : Attribute
    {
        protected string _name;
        protected int sort;

        public FieldAttribute(string name)
        {
            _name = name;
        }
        public string Name => _name;
        public int Sort { get => sort; set => sort = value; }

        public abstract string Template { get; }

        public abstract IField ConstructField(object target, PropertyInfo property);
    }

    public abstract class FieldAttribute<T> : FieldAttribute
    {
        protected FieldAttribute(string name) : base(name)
        {
        }

        public override IField ConstructField(object target, PropertyInfo property)
        {
            return new Field<T>(_name, property, target, Template)
                .WithDefault(IsDefault, GetDefault);
        }

        protected abstract T? GetDefault();

        protected abstract bool IsDefault(T? value);
    }
}