namespace Trebuchet.ViewModels.InnerContainer;

public interface IOnBoardingProgress
{
    string Title { get; }
    string Description { get; }
    double CurrentProgress { get; }
    bool IsIndeterminate { get; }
}