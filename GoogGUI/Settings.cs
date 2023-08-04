using Goog;
using GoogGUI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public class Settings : FieldEditor
    {
        private Config _config;
        private List<Field> _fields = new List<Field>();

        public Settings(Config config)
        {
            _config = config;
            _fields = new List<Field>
            {
                new Field("Install path", "InstallPath", _config, string.Empty, "DirectoryField", OnValueChanged),
                new Field("Client path", "ClientPath", _config, string.Empty, "DirectoryField", OnValueChanged),
                new Field("Manage Servers", "ManageServers", _config, false, "ToggleField", OnValueChanged)
            };
        }

        public event EventHandler? ConfigChanged;

        public override List<Field> Fields { get => _fields; set => _fields = value; }

        protected virtual void OnConfigChanged()
        {
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnValueChanged(Field field, object? e)
        {
            PropertyInfo? property = _config.GetType().GetProperty(field.Property);
            if (property == null)
                throw new Exception($"Could not find property {field.Property}");

            property.SetValue(_config, e);
            _config.SaveConfig();
            OnConfigChanged();
        }
    }
}