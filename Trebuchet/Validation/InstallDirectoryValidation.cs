using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrebuchetLib;

namespace Trebuchet.Validation
{
    public class InstallDirectoryValidation : BaseValidation<string>
    {
        public override Task<bool> IsValid(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                LastError = string.Empty;
                return Task.FromResult(true);
            }
            value = Config.ResolveInstallPath(value);
            if (!Utils.Utils.ValidateInstallDirectory(value, out var installDirError))
            {
                LastError = installDirError;
                return Task.FromResult(false);
            }
            if (!Tools.ValidateDirectoryUac(value))
            {
                StrongReferenceMessenger.Default.Send(new UACPromptRequest(value));
                LastError = string.Empty;
                return Task.FromResult(true);
            }
            return Task.FromResult(true);
        }
    }
}