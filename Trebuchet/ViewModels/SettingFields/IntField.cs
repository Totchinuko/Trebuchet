using System;

namespace Trebuchet.ViewModels.SettingFields
{
    public class IntField(int minimum, int maximum) : Field<IntField, int>(0)
    {
        public int Maximum { get; set; } = maximum;

        public int Minimum { get; set; } = minimum;
    }
}