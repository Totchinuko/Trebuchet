using Goog;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace GoogGUI
{
    public class ClientSettings : INotifyPropertyChanging, ITemplateHolder, IFieldEditor
    {
        private Config _config;
        private List<IField> _fields;
        private List<RequiredCommand> _requiredActions = new List<RequiredCommand>();

        public ClientSettings(Config config)
        {
            _config = config;
            _fields = new List<IField>()
            {
            };
        }

        public event PropertyChangingEventHandler? PropertyChanging;

        public List<IField> Fields => _fields;

        public List<RequiredCommand> RequiredActions => _requiredActions;

        public DataTemplate Template => (DataTemplate)Application.Current.Resources["FieldEditor"];
    }
}