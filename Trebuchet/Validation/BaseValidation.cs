using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Trebuchet.Validation;

namespace Trebuchet
{
    [JsonDerivedType(typeof(InstallDirectoryValidation), "InstallDirectory")]
    [JsonDerivedType(typeof(ConanGameDirectoryValidation), "GameDirectory")]
    public abstract class BaseValidation<T>
    {
        public abstract bool IsValid(T? value, out string errorMessage);
    }
}