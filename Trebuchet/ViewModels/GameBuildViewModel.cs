using System.Reactive;
using ReactiveUI;

namespace Trebuchet.ViewModels;

public class GameBuildViewModel : ReactiveObject
{
    private readonly App _app;

    public GameBuildViewModel(App app)
    {
        _app = app;
        LiveCommand = ReactiveCommand.Create(OnLiveClicked);
        TestLiveCommand = ReactiveCommand.Create(OnTestLiveClicked);
    }

    public ReactiveCommand<Unit, Unit> LiveCommand { get; }
    public ReactiveCommand<Unit, Unit> TestLiveCommand { get; }
        
    private void OnLiveClicked()
    {
        _app.OpenApp(false);
    }

    private void OnTestLiveClicked()
    {
        _app.OpenApp(true);
    }
}