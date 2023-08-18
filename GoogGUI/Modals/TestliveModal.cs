using Goog;
using GoogLib;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class TestliveModal : BaseModal
    {
        private bool _madeASelection;

        public TestliveModal() : base()
        {
            LiveCommand = new SimpleCommand(OnLiveClicked);
            TestLiveCommand = new SimpleCommand(OnTestLiveClicked);
        }

        public override bool CloseDisabled => false;

        public ICommand LiveCommand { get; private set; }

        public override int ModalHeight => 400;

        public override string ModalTitle => "Game Build";

        public override int ModalWidth => 400;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["TestliveSelection"];

        public ICommand TestLiveCommand { get; private set; }

        public static void OpenApp(bool testlive)
        {
            Log.Write($"Selecting {(testlive?"testlive":"live")}", LogSeverity.Info);
            Config config = Config.LoadConfig(Config.GetPath(testlive));
            UIConfig uiConfig = UIConfig.LoadConfig(UIConfig.GetPath(testlive));
            App.UseSoftwareRendering = !uiConfig.UseHardwareAcceleration;

            MainWindow mainWindow = new MainWindow(config, uiConfig);
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

        public override void OnWindowClose()
        {
            if (!_madeASelection)
                Application.Current.Shutdown();
        }

        private void OnLiveClicked(object? obj)
        {
            _madeASelection = true;
            OpenApp(false);
            _window.Close();
        }

        private void OnTestLiveClicked(object? obj)
        {
            _madeASelection = true;
            OpenApp(true);
            _window.Close();
        }
    }
}