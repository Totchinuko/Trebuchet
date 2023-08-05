using Goog;
using GoogGUI.Templates.Converters;
using GoogLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GoogGUI
{
    public class Settings : FieldEditor
    {
        private Config _config;
        private List<IField> _fields = new List<IField>();
        private ObservableIntanceListConverter _instanceConverter = new ObservableIntanceListConverter();

        public Settings(Config config)
        {
            _config = config;
            TrulyObservableCollection<ObservableServerInstance> serverInstances = (TrulyObservableCollection<ObservableServerInstance>)_instanceConverter.Convert(
                _config.ServerInstances, typeof(TrulyObservableCollection<ObservableServerInstance>), null, null);

            _fields = new List<IField>
            {
                new Field<string>("Install path", "InstallPath", _config.InstallPath, "DirectoryField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => x?.Equals(string.Empty)??true, () => string.Empty),
                new Field<string>("Client path", "ClientPath", _config.ClientPath, "DirectoryField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => x?.Equals(string.Empty)??true, () => string.Empty),
                new Field<bool>("Manage Servers", "ManageServers", _config.ManageServers, "ToggleField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => !x, () => false),
                new Field<TrulyObservableCollection<ObservableServerInstance>>("Server Instances", "ServerInstances", serverInstances, "InstancesField")
                    .WhenChanged(OnInstanceValueChanged)
                    .WithDefault((x) => x?.Count == 1, () => new TrulyObservableCollection<ObservableServerInstance>{ new ObservableServerInstance(new ServerInstance()) }),
            };
        }

        public event EventHandler? ConfigChanged;

        public override List<IField> Fields { get => _fields; set => _fields = value; }

        protected virtual void OnConfigChanged()
        {
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnInstanceValueChanged(string name, object? value)
        {
            if (value == null)
            {
                OnValueChanged(name, value);
                return;
            }
            OnValueChanged(name, _instanceConverter.ConvertBack(value, typeof(List<ServerInstance>), null, null));
        }

        private void OnValueChanged(string name, object? value)
        {
            PropertyInfo? property = _config.GetType().GetProperty(name);
            if (property == null)
                throw new Exception($"Could not find property {name}");

            property.SetValue(_config, value);
            _config.SaveConfig();
            OnConfigChanged();
        }
    }
}