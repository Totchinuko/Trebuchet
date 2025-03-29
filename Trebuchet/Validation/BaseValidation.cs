using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Trebuchet.Validation
{
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