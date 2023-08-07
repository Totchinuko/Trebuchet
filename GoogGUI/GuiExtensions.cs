using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogGUI
{
    public static class GuiExtensions
    {
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(GuiExtensions),
            new PropertyMetadata(default(CornerRadius)));

        public static void SetCornerRadius(UIElement element, CornerRadius value)
        {
            element.SetValue(CornerRadiusProperty, value);
        }

        public static CornerRadius GetCornerRadius(UIElement element)
        {
            return (CornerRadius)element.GetValue(CornerRadiusProperty);
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon",
            typeof(ImageSource),
            typeof(GuiExtensions),
            new PropertyMetadata(default(ImageSource)));

        public static void SetIcon(UIElement element, ImageSource value)
        {
            element.SetValue(IconProperty, value);
        }

        public static ImageSource GetIcon(UIElement element)
        {
            return (ImageSource)element.GetValue(IconProperty);
        }

        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.RegisterAttached(
            "IconSize",
            typeof(double),
            typeof(GuiExtensions),
            new PropertyMetadata(default(double)));

        public static void SetIconSize(UIElement element, double value)
        {
            element.SetValue(IconSizeProperty, value);
        }

        public static double GetIconSize(UIElement element)
        {
            return (double)element.GetValue(IconSizeProperty);
        }

        public static readonly DependencyProperty AccentProperty = DependencyProperty.RegisterAttached(
            "Accent",
            typeof(bool),
            typeof(GuiExtensions),
            new PropertyMetadata(default(bool)));

        public static void SetAccent(UIElement element, bool value)
        {
            element.SetValue(AccentProperty, value);
        }

        public static bool GetAccent(UIElement element)
        {
            return (bool)element.GetValue(AccentProperty);
        }

        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.RegisterAttached(
                "IsDragging",
                typeof(bool),
                typeof(GuiExtensions),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

        public static bool GetIsDragging(DependencyObject source)
        {
            return (bool)source.GetValue(IsDraggingProperty);
        }

        public static void SetIsDragging(DependencyObject target, bool value)
        {
            target.SetValue(IsDraggingProperty, value);
        }

        public static readonly DependencyProperty IsDraggedOverProperty = DependencyProperty.RegisterAttached(
                "IsDraggedOver",
                typeof(bool),
                typeof(GuiExtensions),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

        public static bool GetIsDraggedOver(DependencyObject source)
        {
            return (bool)source.GetValue(IsDraggedOverProperty);
        }

        public static void SetIsDraggedOver(DependencyObject target, bool value)
        {
            target.SetValue(IsDraggedOverProperty, value);
        }

        public static IGuiField SetField(this IGuiField field, object target, string property, object? defaultValue)
        {
            if (string.IsNullOrEmpty(property))
                throw new NullReferenceException("Property is not set to a valid name");
            PropertyInfo? prop = target.GetType().GetProperty(property);
            if (prop == null)
                throw new NullReferenceException($"{property} was not found on {target.GetType()}");

            field.SetField(property, prop.GetValue(target), defaultValue);
            return field;
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

        public static void SetParentValue<TParent>(this DependencyObject child, DependencyProperty property, object value) where TParent : DependencyObject
        {
            if (child.TryGetParent(out TParent? parent))
            {
                parent.SetValue(property, value);
            }
        }
    }
}