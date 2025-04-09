﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using VisualExtensions = Avalonia.VisualTree.VisualExtensions;

namespace TrebuchetUtils
{
    public static class GuiExtensions
    {
        private static readonly Dictionary<Uri, Bitmap> Cache = [];
        private static readonly HttpClient HttpClient = new();

        public static IEnumerable<T> FindVisualChildren<T>(Visual depObj) where T : Visual
        {
            foreach (var child in VisualExtensions.GetVisualChildren(depObj))
            {
                if (child is T t) yield return t;
                foreach(var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
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
        public static IEnumerable<Visual> FindVisualChildren(Visual depObj)
        {
            foreach (var child in VisualExtensions.GetVisualChildren(depObj))
            {
                yield return child;
                foreach(var childOfChild in FindVisualChildren(child)) yield return childOfChild;
            }
        }
        
        public static void SetParentValue<TParent>(this Visual child, AvaloniaProperty property, object value) where TParent : Visual
        {
            if(child.Parent is TParent)
                child.Parent.SetValue(property, value);
        }
        
        public static async Task<Bitmap> DownloadImage(Uri uri)
        {
            if (Cache.TryGetValue(uri, out var image))
            {
                return image;
            }
            else
            {
                var data = await HttpClient.GetByteArrayAsync(uri);
                var bitmap = new Bitmap(new MemoryStream(data));
                Cache.Add(uri, bitmap);
                return bitmap;
            }
        }

        // See if Avalonia still need a shitty hack like that for links
        // public static void SubscribeToAllHyperlinks(FlowDocument flowDocument)
        // {
        //     var handler = new SimpleCommand(OnHyperlinkClicked);
        //     var hyperlinks = GetVisuals(flowDocument).OfType<Hyperlink>();
        //     foreach (var link in hyperlinks)
        //     {
        //         link.Command = handler;
        //         link.Cursor = Cursors.Hand;
        //         link.ForceCursor = true;
        //     }
        // }
        
        // private static void OnHyperlinkClicked(object? obj)
        // {
        //     if (obj is Uri link)
        //     {
        //         var info = new ProcessStartInfo(link.AbsoluteUri);
        //         info.UseShellExecute = true;
        //         Process.Start(info);
        //     }
        // }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            return new ObservableCollection<T>(enumerable);
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree.
        /// </summary>
        public static bool TryFindChild<T>(Visual parent, string childName, [NotNullWhen(true)] out T? foundChild)
           where T : Visual
        {
            foundChild = null;
            foreach (var child in VisualExtensions.GetVisualChildren(parent))
            {
                if (child is T valid)
                {
                    if (!string.IsNullOrEmpty(childName))
                    {
                        // If the child's name is set for search
                        if (child.Name == childName)
                        {
                            // if the child's name is of the request name
                            foundChild = valid;
                            return true;
                        }
                    }
                    else
                    {
                        foundChild = valid;
                        return true;
                    }
                }

                if (TryFindChild(child, childName, out foundChild))
                    return true;
            }
            return false;
        }

        public static bool TryGetParent<TParent>(this Visual child, [NotNullWhen(true)] out TParent? parent) where TParent : Visual
        {
            var current = child;
            while (current != null && current is not TParent)
            {
                current = current.Parent as Visual;
            }
            if (current is TParent result)
            {
                parent = result;
                return true;
            }

            parent = null;
            return false;
        }
        
        public static bool TryGetParent<TParent>(this Visual child, string name, [NotNullWhen(true)] out TParent? parent) where TParent : Visual
        {
            var current = child;
            while (current != null && (current is not TParent || current.Name != name))
            {
                current = current.Parent as Visual;
            }
            if (current is TParent result && current.Name == name)
            {
                parent = result;
                return true;
            }

            parent = null;
            return false;
        }
    }
}