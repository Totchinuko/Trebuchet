using System.IO;

using CommunityToolkit.Mvvm.Messaging;
using TrebuchetGUILib;

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
            if (!TrebuchetUtils.ValidateGameDirectory(value, out errorMessage))
                return false;

            if (!Tools.ValidateDirectoryUAC(value))
            {
                StrongReferenceMessenger.Default.Send(new UACPromptRequest(value));
                errorMessage = string.Empty;
                return false;
            }

            if (!HandlePotatoes(value))
            {
                errorMessage = App.GetAppText("Validation_PotatoError");
                return false;
            }

            if (!HandleOriginalSavedDirectory(value, out errorMessage))
                return false;

            return true;
        }

        private bool HandleOriginalSavedDirectory(string value, out string errorMessage)
        {
            errorMessage = string.Empty;
            string savedFolder = Path.Combine(value, Config.FolderGameSave);
            if (Tools.IsSymbolicLink(savedFolder)) return true;

            QuestionModal question = new QuestionModal(
                App.GetAppText("Validation_HandleOriginalSavedDirectory_Title"),
                App.GetAppText("Validation_HandleOriginalSavedDirectory"));
            question.ShowDialog();

            if (!question.Result)
                return false;

            Config config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
            string newPath = savedFolder + "_Original";

            try
            {
                Directory.Move(savedFolder, newPath);
            }
            catch
            {
                errorMessage = App.GetAppText("Validation_PotatoError");
                return false;
            }
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