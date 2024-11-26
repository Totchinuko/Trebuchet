using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for IntBox.xaml
    /// </summary>
    public partial class IntBox : UserControl
    {
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(int), typeof(IntBox), new PropertyMetadata(Int32.MaxValue));
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(int), typeof(IntBox), new PropertyMetadata(Int32.MinValue));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(IntBox), new PropertyMetadata(0, OnValueChanged));

        private static readonly Regex _regex = new Regex("[^0-9.-]+");

        public IntBox()
        {
            InitializeComponent();
        }

        public int MaxValue
        {
            get => (int)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public int MinValue
        {
            get => (int)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public int Value
        {
            get => Math.Clamp((int)GetValue(ValueProperty), MinValue, MaxValue);
            set => SetValue(ValueProperty, Math.Clamp(value, MinValue, MaxValue));
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not IntBox box) return;
            int value = (int)e.NewValue;
            box.TextField.Text = value.ToString();
        }

        private bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsTextAllowed(e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        private void TextField_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (int.TryParse(TextField.Text, out int number))
            {
                Value = number;
                TextField.Text = Value.ToString();
            }
            else
            {
                Value = 0;
                TextField.Text = "0";
            }
        }
    }
}