using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GoogGUI.Templates.Converters
{
    internal class IntDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertIntDouble(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertIntDouble(value);
        }

        protected virtual object ConvertIntDouble(object value)
        {
            if (value is int i)
                return (double)i;
            else if (value is double d)
                return (int)d;
            throw new Exception("Can only convert int to double or double to int.");
        }
    }
}
