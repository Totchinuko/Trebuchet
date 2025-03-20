using System;

namespace Trebuchet.SettingFields
{
    public class CpuAffinityField() : Field<long, long>("CpuAffinityField")
    {
        public override bool IsDefault => Value == GetDefaultValue();

        public override void ResetToDefault()
        {
            Value = GetDefaultValue();
        }

        protected override long GetConvert(object? value)
        {
            if (value is not long longValue)
                throw new ArgumentException("value is not long", nameof(value));
            return longValue;
        }

        protected override object? SetConvert(long value)
        {
            return value;
        }

        // flip the first x bit of the long value
        private long GetDefaultValue()
        {
            int maxCpu = Environment.ProcessorCount;
            long mask = 0L;
            for (int i = 0; i < maxCpu; i++)
                mask |= 1L << i;
            return mask;
        }
    }
}