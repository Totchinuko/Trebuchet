using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public class CPUAffinityField : Field<long, long>
    {
        public override bool IsDefault => Value == GetDefaultValue();

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["CPUAffinityField"];

        protected override long GetConvert(object? value)
        {
            if(value is not long longValue)
                throw new ArgumentException("value is not long", nameof(value));
            return longValue;
        }

        protected override void ResetToDefault()
        {
            Value = GetDefaultValue();
        }

        protected override object? SetConvert(long value)
        {
            return value;
        }

        // flip the first x bit of the long value
        private long GetDefaultValue()
        {
            int maxCPU = Environment.ProcessorCount;
            long mask = 0L;
            for (int i = 0; i < maxCPU; i++)
                mask |= 1L << i;
            return mask;
        }
    }
}
