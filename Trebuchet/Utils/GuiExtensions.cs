using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SteamWorksWebAPI;

namespace Trebuchet
{
    internal static class GuiExtensions
    {
        /// <summary>
        /// Asserts that the given assertion is true, and if not, shows an error modal with the given message.
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="message"></param>
        /// <returns>Return false if the assertion failled</returns>
        public static bool Assert(bool assertion, string message)
        {
            if (!assertion)
            {
                new ErrorModal("Error", message).ShowDialog();
                return false;
            }
            return true;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }

        public static string GetAllExceptions(this Exception ex)
        {
            int x = 0;
            string pattern = "EXCEPTION #{0}:\r\n{1}";
            string message = String.Format(pattern, ++x, ex.Message);
            message += "\r\n============\r\n" + ex.StackTrace;
            Exception? inner = ex.InnerException;
            while (inner != null)
            {
                message += "\r\n============\r\n" + String.Format(pattern, ++x, inner.Message);
                message += "\r\n============\r\n" + inner.StackTrace;
                inner = inner.InnerException;
            }
            return message;
        }

        public static string GetEmbededTextFile(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(path) ?? throw new Exception($"Could not find resource {path}."))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetFileVersion()
        {
            if (string.IsNullOrEmpty(System.Environment.ProcessPath))
                return string.Empty;
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Environment.ProcessPath);
            return fvi.FileVersion ?? string.Empty;
        }

        public static IEnumerable<(ulong, ulong)> GetManifestKeyValuePairs(this List<PublishedFile> list)
        {
            foreach (var file in list)
            {
                if (ulong.TryParse(file.HcontentFile, out var manifest))
                    yield return (file.PublishedFileID, manifest);
            }
        }

        public static void RestartProcess(bool testlive, bool asAdmin = false)
        {
            var data = Tools.GetProcess(Environment.ProcessId);
            Process process = new Process();
            process.StartInfo.FileName = data.filename;
            process.StartInfo.Arguments = data.args + (testlive ? " -testlive" : " -live");
            process.StartInfo.UseShellExecute = true;
            if (asAdmin)
                process.StartInfo.Verb = "runas";
            process.Start();
            Application.Current.Shutdown();
        }

        public static void SetParentValue<TParent>(this DependencyObject child, DependencyProperty property, object value) where TParent : DependencyObject
        {
            if (child.TryGetParent(out TParent? parent))
            {
                parent.SetValue(property, value);
            }
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            return new ObservableCollection<T>(enumerable);
        }

        public static bool TryGetParent<TParent>(this DependencyObject child, [NotNullWhen(true)] out TParent? parent) where TParent : DependencyObject
        {
            DependencyObject current = child;
            while (current != null && !(current is TParent))
            {
                current = VisualTreeHelper.GetParent(current);
            }
            if (current is TParent result && result != null)
            {
                parent = result;
                return true;
            }

            parent = default;
            return false;
        }

        public static bool ValidateGameDirectory(string gameDirectory, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(gameDirectory))
            {
                errorMessage = App.GetAppText("InvalidDirectory");
                return false;
            }
            if (!Directory.Exists(gameDirectory))
            {
                errorMessage = App.GetAppText("DirectoryNotFound");
                return false;
            }
            if (!File.Exists(Path.Join(gameDirectory, Config.FolderGameBinaries, Config.FileClientBin)))
            {
                errorMessage = App.GetAppText("GameDirectoryInvalidError");
                return false;
            }
            return true;
        }

        public static bool ValidateInstallDirectory(string installPath, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(installPath))
            {
                errorMessage = App.GetAppText("InvalidDirectory");
                return false;
            }
            if (!Directory.Exists(installPath))
            {
                errorMessage = App.GetAppText("DirectoryNotFound");
                return false;
            }
            if (Regex.IsMatch(installPath, Config.RegexSavedFolder))
            {
                errorMessage = App.GetAppText("InstallPathInGameError");
                return false;
            }
            return true;
        }
    }
}