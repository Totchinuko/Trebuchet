using GoogGUI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace GoogGUI
{
    public interface IField
    {
        public string FieldName { get; set; }

        public bool IsDefault { get; }

        public ICommand ResetCommand { get; }

        public object Template { get; }

        public object? Value { get; set; }

        public static List<IField> BuildFieldList(object target)
        {
            List<IField> fields = new List<IField>();

            Type type = target.GetType();
            var properties = type.GetProperties()
                .Where(prop => prop.GetCustomAttribute<FieldAttribute>() != null)
                .OrderBy(prop => prop.GetCustomAttribute<FieldAttribute>()?.Sort ?? 0);

            foreach (var property in properties)
            {
                FieldAttribute attribute = property.GetCustomAttribute<FieldAttribute>() ?? throw new Exception($"Could not find FieldAttribute on {property.Name}");
                fields.Add(attribute.ConstructField(target, property));
            }

            return fields;
        }
    }
}