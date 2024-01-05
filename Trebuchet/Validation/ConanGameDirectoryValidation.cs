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

            if (!HandlePotatoes(value))
            {
                errorMessage = "Cannot setup the game directory while the game run. Close the game and retry. Bad Potato.";
                return false;
            }

            if (!HandleOriginalSavedDirectory(value))
            {
                errorMessage = string.Empty;
                return false;
            }

            return true;
        }

        private bool HandleOriginalSavedDirectory(string value)
        {
            string savedFolder = Path.Combine(value, Config.FolderGameSave);
            if (Tools.IsSymbolicLink(savedFolder)) return true;

            QuestionModal question = new QuestionModal("Saved Data", "Your game directory contain saved data from your previous use of the game. " +
                "In order to use the features of the launcher, the folder will be renamed and its content copied into a new profile to use with the launcher. All your data will still be under the folder Saved_Original. " +
                "Do you wish to continue ?");
            question.ShowDialog();

            if (question.Result != System.Windows.Forms.DialogResult.Yes)
                return false;

            Config config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
            string newPath = savedFolder + "_Original";
            Directory.Move(savedFolder, newPath);
            ClientProfile Original = ClientProfile.CreateProfile(config, ClientProfile.GetUniqueOriginalProfile(config));
            Original.SaveFile();
            string profileFolder = Path.GetDirectoryName(Original.FilePath) ?? throw new DirectoryNotFoundException($"{Original.FilePath} path is invalid");
            Tools.DeepCopy(newPath, profileFolder);
            return true;
        }

        // Are you fucking running the game? -Potato 2024-
        private bool HandlePotatoes(string value)
        {
            return !Tools.IsProcessRunning(Path.Combine(value, Config.FolderGameBinaries, Config.FileClientBin));
        }
    }
}