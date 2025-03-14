using System.Reflection;

namespace Trebuchet.SettingFields
{
    public class TitleField() : Field("TitleField")
    {
        public override bool IsDefault { get; } = true;
        public override bool UseFieldRow => false;

        public override void RefreshValue()
        {
        }

        public override void RefreshVisibility()
        {
        }

        public override void ResetToDefault()
        {
        }

        public override void SetTarget(object target, PropertyInfo? property = null)
        {
        }
    }
}