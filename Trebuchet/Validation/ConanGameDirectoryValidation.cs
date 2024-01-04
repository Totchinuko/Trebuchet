using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet
{
    public class ConanGameDirectoryValidation : BaseValidation<string>
    {
        public override bool IsValid(string? value, out string errorMessage)
        {
            if (string.IsNullOrEmpty(value))
            {
                errorMessage = string.Empty;
                return true;
            }
            if (!Tools.ValidateGameDirectory(value, out errorMessage))
                return false;

            if (!Tools.ValidateDirectoryUAC(value))
            {
                StrongReferenceMessenger.Default.Send(new UACPromptRequest(value));
                errorMessage = string.Empty;
                return false;
            }

            return true;
        }
    }
}