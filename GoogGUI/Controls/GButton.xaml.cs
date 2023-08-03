using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoogGUI.Controls
{
    /// <summary>
    /// Interaction logic for GButton.xaml
    /// </summary>
    public partial class GButton : UserControl
    {
        public static readonly DependencyProperty ButtonAccentColorProperty = DependencyProperty.Register(
            "ButtonAccentColor",
            typeof(Brush),
            typeof(GButton),
            new PropertyMetadata(Application.Current.Resources["GBlue"], new PropertyChangedCallback(OnStylingChanged))
            );

        public static readonly DependencyProperty ButtonAccentProperty = DependencyProperty.Register(
            "ButtonAccent",
            typeof(bool),
            typeof(GButton),
            new PropertyMetadata(false, new PropertyChangedCallback(OnStylingChanged))
            );

        public static readonly DependencyProperty ButtonBackgroundProperty = DependencyProperty.Register(
            "ButtonBackground",
            typeof(Brush),
            typeof(GButton),
            new PropertyMetadata(
                new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                new PropertyChangedCallback(OnStylingChanged)
                )
            );

        public static readonly DependencyProperty ButtonBorderBrushProperty = DependencyProperty.Register(
            "ButtonBorderBrush",
            typeof(Brush),
            typeof(GButton),
            new PropertyMetadata(
                new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                new PropertyChangedCallback(OnStylingChanged)
                )
            );

        public static readonly DependencyProperty ButtonBorderThicknessProperty = DependencyProperty.Register(
            "ButtonBorderThickness",
            typeof(Thickness),
            typeof(GButton),
            new PropertyMetadata(
                    new Thickness(0),
                    new PropertyChangedCallback(OnStylingChanged)
                )
            );

        public static readonly DependencyProperty ButtonCornerRadiusProperty = DependencyProperty.Register(
            "ButtonCornerRadius",
            typeof(CornerRadius),
            typeof(GButton),
            new PropertyMetadata(new CornerRadius(3), new PropertyChangedCallback(OnStylingChanged))
            );

        public static readonly DependencyProperty ButtonFontSizeProperty = DependencyProperty.Register(
            "ButtonFontSize",
            typeof(double),
            typeof(GButton),
            new PropertyMetadata(14d, new PropertyChangedCallback(OnStylingChanged))
            );

        public static readonly DependencyProperty ButtonForegroundProperty = DependencyProperty.Register(
            "ButtonForeground",
            typeof(Brush),
            typeof(GButton),
            new PropertyMetadata(
                new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                new PropertyChangedCallback(OnStylingChanged)
                )
            );

        public static readonly DependencyProperty ButtonHoverBackgroundProperty = DependencyProperty.Register(
            "ButtonHoverBackground",
            typeof(Brush),
            typeof(GButton),
            new PropertyMetadata(Application.Current.Resources["GPanel"], new PropertyChangedCallback(OnStylingChanged))
            );

        public static readonly DependencyProperty ButtonIconOpacityProperty = DependencyProperty.Register(
            "ButtonIconOpacity",
            typeof(double),
            typeof(GButton),
            new PropertyMetadata(1d, new PropertyChangedCallback(OnStylingChanged))
            );

        public static readonly DependencyProperty ButtonIconProperty = DependencyProperty.Register(
            "ButtonIcon",
            typeof(ImageSource),
            typeof(GButton),
            new PropertyMetadata(
                    null,
                    new PropertyChangedCallback(OnStylingChanged)
                )
            );

        public static readonly DependencyProperty ButtonIconSizeProperty = DependencyProperty.Register(
            "ButtonIconSize",
            typeof(int),
            typeof(GButton),
            new PropertyMetadata(16, new PropertyChangedCallback(OnStylingChanged))
            );

        public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register(
            "ButtonText",
            typeof(string),
            typeof(GButton),
            new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnStylingChanged))
            );

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command",
            typeof(ICommand),
            typeof(GButton),
            new PropertyMetadata(null, new PropertyChangedCallback(OnStylingChanged))
            );

        public GButton()
        {
            InitializeComponent();
        }

        public bool ButtonAccent
        {
            get => (bool)GetValue(ButtonAccentProperty);
            set => SetValue(ButtonAccentProperty, value);
        }

        public Brush ButtonAccentColor
        {
            get => (Brush)GetValue(ButtonAccentColorProperty);
            set => SetValue(ButtonAccentColorProperty, value);
        }

        public Brush? ButtonBackground
        {
            get => (Brush)GetValue(ButtonBackgroundProperty);
            set => SetValue(ButtonBackgroundProperty, value);
        }

        public Brush? ButtonBorderBrush
        {
            get => (Brush)GetValue(ButtonBorderBrushProperty);
            set => SetValue(ButtonBorderBrushProperty, value);
        }

        public Thickness ButtonBorderThickness
        {
            get => (Thickness)GetValue(ButtonBorderThicknessProperty);
            set => SetValue(ButtonBorderThicknessProperty, value);
        }

        public CornerRadius ButtonCornerRadius
        {
            get => (CornerRadius)GetValue(ButtonCornerRadiusProperty);
            set => SetValue(ButtonCornerRadiusProperty, value);
        }

        public double ButtonFontSize
        {
            get => (double)GetValue(ButtonFontSizeProperty);
            set => SetValue(ButtonFontSizeProperty, value);
        }

        public Brush? ButtonForeground
        {
            get => (Brush)GetValue(ButtonForegroundProperty);
            set => SetValue(ButtonForegroundProperty, value);
        }

        public Brush? ButtonHoverBackground
        {
            get => (Brush)GetValue(ButtonHoverBackgroundProperty);
            set => SetValue(ButtonHoverBackgroundProperty, value);
        }

        public ImageSource? ButtonIcon
        {
            get => (ImageSource)GetValue(ButtonIconProperty);
            set => SetValue(ButtonIconProperty, value);
        }

        public double ButtonIconOpacity
        {
            get => (double)GetValue(ButtonIconOpacityProperty);
            set => SetValue(ButtonIconOpacityProperty, value);
        }

        public int ButtonIconSize
        {
            get => (int)GetValue(ButtonIconSizeProperty);
            set => SetValue(ButtonIconSizeProperty, value);
        }

        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public bool IsHovered { get; set; }
        public bool IsPressed { get; set; }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            IsPressed = true;
            UpdateBackground();
            if (Command != null)
                Command.Execute(this);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            IsHovered = true;
            UpdateBackground();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            IsPressed = false;
            IsHovered = false;
            UpdateBackground();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            IsPressed = false;
            UpdateBackground();
        }

        protected virtual void UpdateBackground()
        {
            MainBorder.Background = ButtonAccent ? ButtonHoverBackground : IsHovered ? ButtonHoverBackground : ButtonBackground;
            Opacity = IsPressed ? 0.7d : 1d;
        }

        protected virtual void UpdateStyling()
        {
            UpdateBackground();
            MainBorder.CornerRadius = ButtonCornerRadius;
            MainBorder.BorderBrush = ButtonBorderBrush;
            MainBorder.BorderThickness = ButtonBorderThickness;
            AccentBorder.Visibility = ButtonAccent ? Visibility.Visible : Visibility.Hidden;
            AccentBorder.Background = ButtonAccentColor;
            IconColumn.Width = string.IsNullOrEmpty(ButtonText) ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
            TextColumn.Width = string.IsNullOrEmpty(ButtonText) ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            ImageIcon.Source = ButtonIcon;
            ImageIcon.Width = ButtonIconSize;
            ImageIcon.Height = ButtonIconSize;
            ImageIcon.Opacity = ButtonIconOpacity;
            ImageIcon.Visibility = ButtonIcon == null ? Visibility.Collapsed : Visibility.Visible;
            ImageIcon.Margin = string.IsNullOrEmpty(ButtonText) ? new Thickness(0) : new Thickness(0, 0, 12, 0);
            Textblock.Foreground = ButtonForeground;
            Textblock.FontSize = ButtonFontSize;
            Textblock.Visibility = string.IsNullOrEmpty(ButtonText) ? Visibility.Collapsed : Visibility.Visible;
            TextRun.Text = ButtonText;
        }

        private static void OnStylingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GButton button)
                button.UpdateStyling();
        }
    }
}