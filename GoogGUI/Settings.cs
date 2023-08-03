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
        private List<Field> _fields = new List<Field>();
        private Config _config;

        public Settings(Config config)
        {
            _config = config;
            _fields = new List<Field>
            {
                new Field("Install path", "InstallPath", _config, string.Empty, "DirectoryField"),
                new Field("Client path", "ClientPath", _config, string.Empty, "DirectoryField"),
            };

            foreach (Field field in _fields)
                field.ValueChanged += OnValueChanged;
        }

        public override List<Field> Fields { get => _fields; set => _fields = value; }

        private void OnValueChanged(object? sender, object? e)
        {
            if (sender == null || sender is not Field field)
                return;

            PropertyInfo? property = _config.GetType().GetProperty(field.Property);
            if (property == null)
                throw new Exception($"Could not find property {field.Property}");

            property.SetValue(_config, e);
            _config.SaveConfig();
        }
    }
}