using System.Text.Json.Serialization;

namespace Trebuchet.ViewModels.SettingFields
{
    [JsonDerivedType(typeof(ToggleFieldCondition), "Toggle")]
    [JsonDerivedType(typeof(IntFieldCondition), "Int")]
    public abstract class FieldCondition
    {
        public string Property { get; set; } = string.Empty;

        public abstract bool IsVisible(object? target);
    }
}