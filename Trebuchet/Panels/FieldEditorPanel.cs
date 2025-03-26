﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Messages;
using Trebuchet.SettingFields;

namespace Trebuchet.Panels
{
    public abstract class FieldEditorPanel(string label, string template, string iconPath, PanelPosition position) : 
        Panel(label, string.IsNullOrEmpty(template) ? "FieldEditor" : template, iconPath, position)
    {
        public List<Field> Fields { get; set; } = [];

        public ObservableCollection<RequiredCommand> RequiredActions { get; set; } = [];

        protected virtual void BuildFields(string path, object target, string property = "")
        {
            var fields = Field.BuildFieldList(TrebuchetUtils.Utils.GetEmbeddedTextFile(path), target, string.IsNullOrEmpty(property) ? null : target.GetType().GetProperty(property));
            foreach (var field in fields)
            {
                field.ValueChanged += OnFieldValueChanged;
                Fields.Add(field);
            }
        }

        protected abstract void BuildFields();

        protected virtual void OnValueChanged(string property)
        { }

        protected void RefreshFields()
        {
            if (Fields.Count == 0)
                BuildFields();
            else
                Fields.ForEach(f => f.RefreshValue());
        }

        private void OnFieldValueChanged(object? sender, Field e)
        {
            OnValueChanged(e.Property);
            Fields.ForEach(f => f.RefreshVisibility());
            if (e.RefreshApp)
                StrongReferenceMessenger.Default.Send<PanelRefreshConfigMessage>();
        }
    }
}