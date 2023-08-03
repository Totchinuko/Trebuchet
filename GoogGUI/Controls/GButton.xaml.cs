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
    public partial class GButton : UserControl, INotifyPropertyChanged
    {
        public GButton()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register("ButtonText", typeof(string), typeof(GButton), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ButtonIconProperty = DependencyProperty.Register("ButtonIcon", typeof(ImageSource), typeof(GButton));
        public static readonly DependencyProperty ButtonCornerRadiusProperty = DependencyProperty.Register("ButtonCornerRadius", typeof(CornerRadius), typeof(GButton), new PropertyMetadata(new CornerRadius(3)));
        public static readonly DependencyProperty ButtonHoverBackgroundProperty = DependencyProperty.Register("ButtonHoverBackground", typeof(Brush), typeof(GButton), new PropertyMetadata(Application.Current.Resources["GPanel"]));
        public static readonly DependencyProperty ButtonForegroundProperty = DependencyProperty.Register("ButtonForeground", typeof(Brush), typeof(GButton), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255,255,255))));
        public static readonly DependencyProperty ButtonBorderBrushProperty = DependencyProperty.Register("ButtonBorderBrush", typeof(Brush), typeof(GButton));
        public static readonly DependencyProperty ButtonBackgroundProperty = DependencyProperty.Register("ButtonBackground", typeof(Brush), typeof(GButton));
        public static readonly DependencyProperty ButtonBorderThicknessProperty = DependencyProperty.Register("ButtonBorderThickness", typeof(Thickness), typeof(GButton));
        public static readonly DependencyProperty ButtonFontSizeProperty = DependencyProperty.Register("ButtonFontSize", typeof(double), typeof(GButton), new PropertyMetadata(14d));
        public static readonly DependencyProperty ButtonIconSizeProperty = DependencyProperty.Register("ButtonIconSize", typeof(int), typeof(GButton), new PropertyMetadata(16));
        public static readonly DependencyProperty ButtonIconOpacityProperty = DependencyProperty.Register("ButtonIconOpacity", typeof(double), typeof(GButton), new PropertyMetadata(1d));
        public static readonly DependencyProperty ButtonAccentProperty = DependencyProperty.Register("ButtonAccent", typeof(bool), typeof(GButton), new PropertyMetadata(false));
        public static readonly DependencyProperty ButtonAccentColorProperty = DependencyProperty.Register("ButtonAccentColor", typeof(Brush), typeof(GButton), new PropertyMetadata(Application.Current.Resources["GBlue"]));
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(GButton), new PropertyMetadata(null));

        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }
        public ImageSource? ButtonIcon
        {
            get => (ImageSource)GetValue(ButtonIconProperty);
            set => SetValue(ButtonIconProperty, value);
        }
        public CornerRadius ButtonCornerRadius
        {
            get => (CornerRadius)GetValue(ButtonCornerRadiusProperty);
            set => SetValue(ButtonCornerRadiusProperty, value);
        }
        public Brush? ButtonBackground
        {
            get => (Brush)GetValue(ButtonBackgroundProperty);
            set => SetValue(ButtonBackgroundProperty, value);
        }
        public Brush? ButtonHoverBackground
        {
            get => (Brush)GetValue(ButtonHoverBackgroundProperty);
            set => SetValue(ButtonHoverBackgroundProperty, value);
        }
        public Brush? ButtonForeground
        {
            get => (Brush)GetValue(ButtonForegroundProperty);
            set => SetValue(ButtonForegroundProperty, value);
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
        public double ButtonFontSize
        {
            get => (double)GetValue(ButtonFontSizeProperty);
            set => SetValue(ButtonFontSizeProperty, value);
        }        
        public int ButtonIconSize
        {
            get => (int)GetValue(ButtonIconSizeProperty);
            set => SetValue(ButtonIconSizeProperty, value);
        }
        public double ButtonIconOpacity
        {
            get => (double)GetValue(ButtonIconOpacityProperty);
            set => SetValue(ButtonIconOpacityProperty, value);
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
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public bool IsPressed { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            IsPressed = true;
            if (Command != null)
                Command.Execute(this);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPressed"));
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            IsPressed = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPressed"));
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            IsPressed = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPressed"));
        }
    }
}
