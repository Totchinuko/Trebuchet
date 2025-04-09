namespace Trebuchet.ViewModels.SettingFields
{
    public class DirectoryField() : Field<DirectoryField, string>(string.Empty)
    {
        public bool CreateDefaultFolder { get; set; } = false;

        public string DefaultFolder { get; set; } = string.Empty;

    }
}