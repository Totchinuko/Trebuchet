using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SteamWorksWebAPI
{
    public abstract class Query
    {
        public virtual IEnumerable<KeyValuePair<string,string>> GetQueryArguments()
        {
            foreach(var property in GetType().GetProperties())
            {
                string name = property.Name.ToLower();
                if (property.GetCustomAttribute<JsonPropertyNameAttribute>() is var attr && attr != null)
                    name = attr.Name;
                var value = property.GetValue(this);
                yield return new KeyValuePair<string,string>(name, value == null ? string.Empty : value.ToString() ?? string.Empty);
            }
        }

        public string GetFlatQuery()
        {
            return string.Join('&', GetQueryArguments().Select(x => $"{x.Key}={x.Value}"));
        }
    }
}
