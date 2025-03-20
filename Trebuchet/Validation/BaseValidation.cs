using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Trebuchet.Validation
{
    [JsonDerivedType(typeof(InstallDirectoryValidation), "InstallDirectory")]
    [JsonDerivedType(typeof(ConanGameDirectoryValidation), "GameDirectory")]
    public abstract class BaseValidation
    {
        protected BaseValidation()
        { }
    }

    public abstract class BaseValidation<T> : BaseValidation
    {
        public string LastError { get; protected set; } = string.Empty;
        
        public abstract Task<bool> IsValid(T? value);
    }
}