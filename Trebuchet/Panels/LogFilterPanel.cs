﻿using CommunityToolkit.Mvvm.Messaging;
using System.Windows;

namespace Trebuchet
{
    public class LogFilterPanel : Panel
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();

        public LogFilterPanel()
        {
            LoadPanel();
        }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["LogFilterPanel"];

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
                   _config.ServerInstanceCount > 0;
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        private void LoadPanel()
        {
        }
    }
}