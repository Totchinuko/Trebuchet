using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetUtils
{
    public class LabelOrdering : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x == null || y == null) return 0;
            return ((ITag)x).Name.CompareTo(((ITag)y).Name);
        }
    }
}