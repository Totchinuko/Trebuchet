﻿using System.Reflection;
using System.Windows;

namespace GoogGUI.SettingFields
{
    public class TitleField : Field
    {
        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["TitleField"];

        public override bool UseFieldRow => false;

        public override void RefreshValue()
        {
        }

        public override void RefreshVisibility()
        {
        }

        public override void SetTarget(object target, PropertyInfo? property = null)
        {
        }
    }
}