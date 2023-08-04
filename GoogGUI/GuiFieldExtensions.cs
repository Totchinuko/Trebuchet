using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI
{
    public static class GuiFieldExtensions
    {
        public static IGuiField SetField(this IGuiField field, object target, string property, object? defaultValue)
        {
            if (string.IsNullOrEmpty(property))
                throw new NullReferenceException("Property is not set to a valid name");
            PropertyInfo? prop = target.GetType().GetProperty(property);
            if (prop == null)
                throw new NullReferenceException($"{property} was not found on {target.GetType()}");

            field.SetField(property, prop.GetValue(target), defaultValue);
            return field;
        }

        public static string GetAllExceptions(this Exception ex)
        {
            int x = 0;
            string pattern = "EXCEPTION #{0}:\r\n{1}";
            string message = String.Format(pattern, ++x, ex.Message);
            message += "\r\n============\r\n" + ex.StackTrace;
            Exception? inner = ex.InnerException;
            while (inner != null)
            {
                message += "\r\n============\r\n" + String.Format(pattern, ++x, inner.Message);
                message += "\r\n============\r\n" + inner.StackTrace;
                inner = inner.InnerException;
            }
            return message;
        }
    }
}