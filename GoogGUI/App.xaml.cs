﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);
            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(OnApplicationThreadException);
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            new ExceptionModal(e.Exception).ShowDialog();
        }

        private void OnApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            new ExceptionModal(e.Exception).ShowDialog();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            new ExceptionModal(((Exception)e.ExceptionObject)).ShowDialog();
        }
    }
}