using System.Reflection;

namespace Trebuchet.ViewModels.SettingFields
{
    public class TitleField() : Field("TitleField", false)
    {
        public override bool IsDefault { get; } = true;

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