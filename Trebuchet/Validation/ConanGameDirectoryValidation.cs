using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetLib;
using TrebuchetUtils.Modals;

namespace Trebuchet.Validation
{
    public class ConanGameDirectoryValidation : BaseValidation<string>
    {
        public override async Task<bool> IsValid(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            if (!Utils.Utils.ValidateGameDirectory(value, out string errorMessage))
            {
                LastError = errorMessage;
                return false;
            }

            if (!Tools.ValidateDirectoryUac(value))
            {
                StrongReferenceMessenger.Default.Send(new UACPromptRequest(value));
                return false;
            }

            if (!HandlePotatoes(value))
            {
                LastError = App.GetAppText("Validation_PotatoError");
                return false;
            }

            LastError = await HandleOriginalSavedDirectory(value);
            if (!string.IsNullOrEmpty(LastError))
                return false;

            return true;
        }

        private async Task<string> HandleOriginalSavedDirectory(string value)
        {
            string savedFolder = Path.Combine(value, Config.FolderGameSave);
            if (Tools.IsSymbolicLink(savedFolder)) return string.Empty;

            QuestionModal question = new QuestionModal(
                App.GetAppText("Validation_HandleOriginalSavedDirectory_Title"),
                App.GetAppText("Validation_HandleOriginalSavedDirectory"));
            await question.OpenDialogueAsync();

            if (!question.Result)
                return "Canceled";

            Config config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
            string newPath = savedFolder + "_Original";

            try
            {
                Directory.Move(savedFolder, newPath);
            }
            catch
            {
                return App.GetAppText("Validation_PotatoError");
            }
            ClientProfile original = ClientProfile.CreateProfile(config, ClientProfile.GetUniqueOriginalProfile(config));
            original.SaveFile();
            string profileFolder = Path.GetDirectoryName(original.FilePath) ?? throw new DirectoryNotFoundException($"{original.FilePath} path is invalid");
            Tools.DeepCopy(newPath, profileFolder);
            return string.Empty;
        }

        // Are you fucking running the game? -Potato 2024-
        private bool HandlePotatoes(string value)
        {
            return !Tools.IsProcessRunning(Path.Combine(value, Config.FolderGameBinaries, Config.FileClientBin));
        }
    }
}