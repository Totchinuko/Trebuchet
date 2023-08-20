using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yuu.Ini;

namespace Trebuchet
{
    public static class IniDocumentExtensions
    {
        public static void SetParameter(this IniSection section, string parameter, string value)
        {
            section.GetParameters(parameter).ForEach(section.Remove);
            section.InsertParameter(0,parameter, value);
        }

        public static IniSection GetSection(this IniDocument document, string section)
        {
            if (document.HasSection(section))
                return document.GetSections(section)[0];
            document.AddSection(section);
            return document.GetSections(section)[0];
        }

        public static string GetValue(this IniSection section, string parameter, string defaultValue = "")
        {
            var parameters = section.GetParameters(parameter);            
            if (parameters.Count == 0)
                return defaultValue;
            return parameters[parameters.Count - 1].Value;
        }

        public static bool HasSection(this IniDocument document, string section)
        {
            return document.GetSections(section).Count > 0;
        }
    }
}
