using Goog;
using GoogGUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public class Settings : FieldEditor
    {
        private List<Field> _fields = new List<Field>();

        public Settings(Config config)
        {
            Type type = typeof(Config);
            _fields = new List<Field>
            {
                new Field("Install path", "InstallPath", config, string.Empty, "DirectoryField"),
                new Field("Client path", "ClientPath", config, string.Empty, "DirectoryField"),
            };
        }

        public override List<Field> Fields { get => _fields; set => _fields = value; }
    }
}