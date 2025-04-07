using System;

namespace Trebuchet.ViewModels.SettingFields
{
    public class ToggleField : Field<ToggleField, bool>
    {
        public ToggleField() : base(false)
        {
            DisplayGenericDescription = false;
        }
    }
}