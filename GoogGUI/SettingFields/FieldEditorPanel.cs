using Goog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public abstract class FieldEditorPanel : Panel
    {
        private List<Field> _fields = new List<Field>();
        private ObservableCollection<RequiredCommand> _requiredActions = new ObservableCollection<RequiredCommand>();

        protected FieldEditorPanel(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
        }

        protected virtual void BuildFields(string path, object target, string property = "")
        {
            var fields = Field.BuildFieldList(GuiExtensions.GetEmbededTextFile(path), target, string.IsNullOrEmpty(property) ? null : target.GetType().GetProperty(property));
            foreach(var field in fields)
            {
                field.PropertyChanged += OnFieldPropertyChanged;
                _fields.Add(field);
            }
        }

        protected abstract void BuildFields();

        protected void RefreshFields()
        {
            if (_fields.Count == 0)
                BuildFields();
            else
                _fields.ForEach(f => f.RefreshValue());
        }

        private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Value") return;
            if (sender is not Field field) return;

            OnValueChanged(field.Property);

            if (field.RefreshApp)
                OnAppConfigurationChanged();
        }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["FieldEditor"];

        public List<Field> Fields { get => _fields; set => _fields = value; }

        public ObservableCollection<RequiredCommand> RequiredActions { get => _requiredActions; set => _requiredActions = value; }

        protected virtual void OnValueChanged(string property) { }
    }
}
