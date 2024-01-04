using SteamWorksWebAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Windows;
using System.Windows.Media;

namespace Trebuchet
{
    internal static class GuiExtensions
    {
        public static readonly DependencyProperty AccentProperty = DependencyProperty.RegisterAttached(
            "Accent",
            typeof(bool),
            typeof(GuiExtensions),
            new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
                    "CornerRadius",
            typeof(CornerRadius),
            typeof(GuiExtensions),
            new PropertyMetadata(default(CornerRadius)));

        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon",
            typeof(ImageSource),
            typeof(GuiExtensions),
            new PropertyMetadata(default(ImageSource)));

        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.RegisterAttached(
            "IconSize",
            typeof(double),
            typeof(GuiExtensions),
            new PropertyMetadata(default(double)));

        public static readonly DependencyProperty IsDraggedOverProperty = DependencyProperty.RegisterAttached(
                "IsDraggedOver",
                typeof(bool),
                typeof(GuiExtensions),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.RegisterAttached(
                "IsDragging",
                typeof(bool),
                typeof(GuiExtensions),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

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

        public static bool GetAccent(UIElement element)
        {
            return (bool)element.GetValue(AccentProperty);
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

        public static CornerRadius GetCornerRadius(UIElement element)
        {
            return (CornerRadius)element.GetValue(CornerRadiusProperty);
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

        public static ImageSource GetIcon(UIElement element)
        {
            return (ImageSource)element.GetValue(IconProperty);
        }

        public static double GetIconSize(UIElement element)
        {
            return (double)element.GetValue(IconSizeProperty);
        }

        public static bool GetIsDraggedOver(DependencyObject source)
        {
            return (bool)source.GetValue(IsDraggedOverProperty);
        }

        public static bool GetIsDragging(DependencyObject source)
        {
            return (bool)source.GetValue(IsDraggingProperty);
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

        public static void SetAccent(UIElement element, bool value)
        {
            element.SetValue(AccentProperty, value);
        }

        public static void SetCornerRadius(UIElement element, CornerRadius value)
        {
            element.SetValue(CornerRadiusProperty, value);
        }

        public static void SetIcon(UIElement element, ImageSource value)
        {
            element.SetValue(IconProperty, value);
        }

        public static void SetIconSize(UIElement element, double value)
        {
            element.SetValue(IconSizeProperty, value);
        }

        public static void SetIsDraggedOver(DependencyObject target, bool value)
        {
            target.SetValue(IsDraggedOverProperty, value);
        }

        public static void SetIsDragging(DependencyObject target, bool value)
        {
            target.SetValue(IsDraggingProperty, value);
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
    }
}