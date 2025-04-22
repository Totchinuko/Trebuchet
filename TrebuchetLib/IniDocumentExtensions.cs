using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Yuu.Ini;

namespace TrebuchetLib
{
    public static class IniDocumentExtensions
    {
        public static IniSection GetSection(this IniDocument document, string section)
        {
            if (document.HasSection(section) && document.GetSections(section).Count > 0)
                return document.GetSections(section)[0];
            document.AddSection(section);
            return document.GetSections(section)[0];
        }

        public static string GetValue(this IniSection section, string parameter, string defaultValue = "")
        {
            if (section.TryGetValue(parameter, out string value))
                return value;
            return defaultValue;
        }

        public static bool GetValue(this IniSection section, string parameter, bool defaultValue)
        {
            if (section.TryGetValue(parameter, out string value))
                if (bool.TryParse(value, out bool result))
                    return result;
            return defaultValue;
        }

        public static int GetValue(this IniSection section, string parameter, int defaultValue)
        {
            if (section.TryGetValue(parameter, out string value))
                if (int.TryParse(value, out int result))
                    return result;
            return defaultValue;
        }

        public static bool HasSection(this IniDocument document, string section)
        {
            return document.GetSections(section).Count > 0;
        }

        public static void SetParameter(this IniSection section, string parameter, string value)
        {
            section.GetParameters(parameter).ForEach(section.Remove);
            section.InsertParameter(0, parameter, value);
        }

        public static bool TryGetValue(this IniSection section, string parameter, out string value)
        {
            var parameters = section.GetParameters(parameter);
            if (parameters.Count == 0)
            {
                value = string.Empty;
                return false;
            }
            value = parameters[parameters.Count - 1].Value;
            return true;
        }
    }
}