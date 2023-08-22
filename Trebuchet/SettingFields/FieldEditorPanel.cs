using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Trebuchet.Messages;

namespace Trebuchet
{
    public abstract class FieldEditorPanel : Panel
    {
        private List<Field> _fields = new List<Field>();
        private ObservableCollection<RequiredCommand> _requiredActions = new ObservableCollection<RequiredCommand>();

        public List<Field> Fields { get => _fields; set => _fields = value; }

        public ObservableCollection<RequiredCommand> RequiredActions { get => _requiredActions; set => _requiredActions = value; }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["FieldEditor"];

        protected virtual void BuildFields(string path, object target, string property = "")
        {
            var fields = Field.BuildFieldList(GuiExtensions.GetEmbededTextFile(path), target, string.IsNullOrEmpty(property) ? null : target.GetType().GetProperty(property));
            foreach (var field in fields)
            {
                field.ValueChanged += OnFieldValueChanged;
                _fields.Add(field);
            }
        }

        protected abstract void BuildFields();

        protected virtual void OnValueChanged(string property)
        { }

        protected void RefreshFields()
        {
            if (_fields.Count == 0)
                BuildFields();
            else
                _fields.ForEach(f => f.RefreshValue());
        }

        private void OnFieldValueChanged(object? sender, Field e)
        {
            OnValueChanged(e.Property);
            _fields.ForEach(f => f.RefreshVisibility());
            if (e.RefreshApp)
                StrongReferenceMessenger.Default.Send<PanelRefreshConfigMessage>();
        }
    }
}