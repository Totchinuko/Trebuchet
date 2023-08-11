using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yuu.Ini;

namespace GoogLib
{
    public static class IniDocumentExtensions
    {
        public static void SetParameter(this IniSection section, string parameter, string value)
        {
            section.GetParameters(parameter).ForEach(section.Remove);
            section.AddParameter(parameter, value);
        }

        public static IniSection GetSection(this IniDocument document, string section)
        {
            if (document.HasSection(section))
                return document.GetSections(section)[0];
            document.AddSection(section);
            return document.GetSections(section)[0];
        }

        public static bool HasSection(this IniDocument document, string section)
        {
            return document.GetSections().Count > 0;
        }
    }
}
