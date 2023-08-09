using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Goog
{
    internal class PrivateConstructorContractResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

            if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null)
            {
                if (jsonTypeInfo.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0)
                {
                    // The type doesn't have public constructors
                    jsonTypeInfo.CreateObject = () => Activator.CreateInstance(jsonTypeInfo.Type, true) ?? throw new NullReferenceException($"Could not instantiate a class of type {jsonTypeInfo.Type}");
                }
            }

            return jsonTypeInfo;
        }
    }
}
