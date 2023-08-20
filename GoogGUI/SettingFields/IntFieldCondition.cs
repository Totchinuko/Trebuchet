using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI.SettingFields
{
    public class IntFieldCondition : FieldCondition
    {
        public int Value { get; set; }

        public override bool IsVisible(object? target)
        {
            Type type = target?.GetType() ?? throw new ArgumentNullException(nameof(target));
            var property = type.GetProperty(Property) ?? throw new ArgumentException($"Property {Property} does not exist on type {type.Name}");
            if(property.GetValue(target) is not int intValue) throw new ArgumentException($"Property {Property} is not a int on type {type.Name}");
            return intValue == Value;
        }
    }
}
