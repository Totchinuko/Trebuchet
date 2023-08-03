using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI
{
    public interface IGuiField
    {
        event EventHandler<object?>? ValueChanged;

        string FieldName { get; set; }
        bool IsDefault { get; }
        string Property { get; }

        object? GetField();

        void ResetToDefault();
        void SetField(string property, object? value, object? defaultValue);
        bool Validate();
    }
}
