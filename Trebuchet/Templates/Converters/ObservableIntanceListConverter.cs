using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Trebuchet.Templates.Converters
{
    public class ObservableIntanceListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is List<ServerInstance> instances)
                return new TrulyObservableCollection<ObservableServerInstance>(
                    instances.ConvertAll(
                        new Converter<ServerInstance, ObservableServerInstance>(ConvertInstance)
                        )
                    );
            throw new Exception($"Cannot convert {value.GetType()} into TrulyObservableCollection<ObservableServerInstance>");
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is TrulyObservableCollection<ObservableServerInstance> observable)
                return observable.ToList().ConvertAll(new Converter<ObservableServerInstance, ServerInstance>(ConvertInstance));
            throw new Exception($"Cannot convert {value.GetType()} into List<ServerInstance>");
        }

        private ObservableServerInstance ConvertInstance(ServerInstance input)
        {
            return new ObservableServerInstance(input);
        }

        private ServerInstance ConvertInstance(ObservableServerInstance input)
        {
            return input.Instance;
        }
    }
}