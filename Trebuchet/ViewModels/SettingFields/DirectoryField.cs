namespace Trebuchet.ViewModels.SettingFields
{
    public class DirectoryField() : TextField("DirectoryField")
    {
        public bool CreateDefaultFolder { get; set; } = false;

        public string DefaultFolder { get; set; } = string.Empty;

    }
}