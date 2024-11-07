using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TrebuchetGUILib;

/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 Totchinuko https://github.com/Totchinuko
/// Full license text: LICENSE.txt at the project root

namespace Trebuchet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : TrebuchetBaseApp
    {
        public static string APIKey { get; private set; } = string.Empty;

        public static Dictionary<string, string> AppText { get; set; } = new Dictionary<string, string>();

        public static UIConfig Config { get; private set; } = default!;

        public static string GetAppText(string key, params object[] args)
        {
            if (AppText.TryGetValue(key, out var text)) return string.Format(text, args);
            return $"<INVALID_{key}>";
        }

        public static void OpenApp(bool testlive, bool catapult)
        {
            Log.Write($"Selecting {(testlive ? "testlive" : "live")}", LogSeverity.Info);

            Config = UIConfig.LoadConfig(UIConfig.GetPath(testlive));

            TrebuchetApp app = new TrebuchetApp(testlive, catapult);
            MainWindow mainWindow = new MainWindow(app);
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ReadAppText();
            ReadSettings();

            if (e.Args.Length > 0)
            {
                if (e.Args.Contains("-testlive"))
                {
                    OpenApp(true, e.Args.Contains("-catapult"));
                    return;
                }
                else if (e.Args.Contains("-live"))
                {
                    OpenApp(false, e.Args.Contains("-catapult"));
                    return;
                }
            }
            TestliveModal modal = new TestliveModal(e.Args.Contains("-catapult"));
            Current.MainWindow = modal.Window;
            modal.ShowDialog();
        }

        private void ReadAppText()
        {
            var node = JsonSerializer.Deserialize<JsonNode>(GuiExtensions.GetEmbededTextFile("Trebuchet.Data.AppText.json"));
            if (node == null) return;

            AppText.Clear();
            foreach (var n in node.AsObject())
                AppText.Add(n.Key, n.Value?.GetValue<string>() ?? $"<INVALID_{n.Key}>");
        }

        private void ReadSettings()
        {
            var node = JsonSerializer.Deserialize<JsonNode>(GuiExtensions.GetEmbededTextFile("Trebuchet.AppSettings.json"));
            if (node == null) return;

            APIKey = node["apikey"]?.GetValue<string>() ?? string.Empty;
        }
    }
}