using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for FloatBox.xaml
    /// </summary>
    public partial class FloatBox : UserControl
    {
        // public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(float), typeof(FloatBox), new PropertyMetadata(float.MaxValue));
        // public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(float), typeof(FloatBox), new PropertyMetadata(float.MinValue));
        // public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(float), typeof(FloatBox), new PropertyMetadata(0f, OnValueChanged));

        private static readonly Regex _regex = new Regex("[^0-9.-]+");

        public FloatBox()
        {
            InitializeComponent();
        }
        //
        // public float MaxValue
        // {
        //     get => (int)GetValue(MaxValueProperty);
        //     set => SetValue(MaxValueProperty, value);
        // }
        //
        // public float MinValue
        // {
        //     get => (int)GetValue(MinValueProperty);
        //     set => SetValue(MinValueProperty, value);
        // }
        //
        // public float Value
        // {
        //     get => MathF.Min(MaxValue, MathF.Max(MinValue, (float)GetValue(ValueProperty)));
        //     set => SetValue(ValueProperty, MathF.Min(MaxValue, MathF.Max(MinValue, value)));
        // }
        //
        // private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        // {
        //     if (d is not FloatBox box) return;
        //     float value = (float)e.NewValue;
        //     box.TextField.Text = value.ToString();
        // }
        //
        // private bool IsTextAllowed(string text)
        // {
        //     return !_regex.IsMatch(text);
        // }
        //
        // private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        // {
        //     if (e.DataObject.GetDataPresent(typeof(String)))
        //     {
        //         String text = (String)e.DataObject.GetData(typeof(String));
        //         if (!IsTextAllowed(text))
        //         {
        //             e.CancelCommand();
        //         }
        //     }
        //     else
        //     {
        //         e.CancelCommand();
        //     }
        // }
        //
        // private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        // {
        //     if (!IsTextAllowed(e.Text))
        //     {
        //         e.Handled = true;
        //         return;
        //     }
        // }
        //
        // private void TextField_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        // {
        //     if (float.TryParse(TextField.Text, out float number))
        //     {
        //         Value = number;
        //         TextField.Text = Value.ToString();
        //     }
        //     else
        //     {
        //         Value = 0;
        //         TextField.Text = "0";
        //     }
        // }
    }
}
