using System;

namespace Trebuchet.ViewModels.SettingFields
{
    public class CpuAffinityField() : Field<CpuAffinityField,long>(0)
    {
        // flip the first x bit of the long value
        public static long DefaultValue()
        {
            int maxCpu = Environment.ProcessorCount;
            long mask = 0L;
            for (int i = 0; i < maxCpu; i++)
                mask |= 1L << i;
            return mask;
        }
    }
}