using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingDirectory : InnerPopup
{
    private readonly Func<string, Validation> _validation;
    private DirectoryInfo? _result;
    private bool _isDirectoryValid;
    private bool _displayError;
    private string _errorMessage;

    public OnBoardingDirectory(string title, string description, Func<string, Validation> validation) : base()
    {
        _validation = validation;
        Title = title;
        Description = description;
        var result = _validation(string.Empty);
        _isDirectoryValid = result.isValid;
        _errorMessage = result.errorMessage;
        SearchDirectoryCommand = new SimpleCommand().Subscribe(OnSearchDirectory);
        ConfirmCommand = new SimpleCommand().Subscribe(Close);
    }

    public string Title { get; }
    public string Description { get; }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public bool DisplayError
    {
        get => _displayError;
        private set => SetField(ref _displayError, value);
    }

    public DirectoryInfo? Result
    {
        get => _result;
        private set
        {
            if(SetField(ref _result, value))
                OnPropertyChanged(nameof(ResultPath));
        }
    }

    public string ResultPath
    {
        get => Result?.FullName ?? string.Empty;
        set
        {
            var result = _validation(value);
            IsDirectoryValid = result.isValid;
            ErrorMessage = result.errorMessage;
            DisplayError = !IsDirectoryValid;
            if(IsDirectoryValid)
                Result = new DirectoryInfo(value);
            else
                Result = null;
        }
    }

    public bool IsDirectoryValid
    {
        get => _isDirectoryValid;
        private set => SetField(ref _isDirectoryValid, value);
    }

    public SimpleCommand SearchDirectoryCommand { get; }
    public SimpleCommand ConfirmCommand { get; }
    
    private async void OnSearchDirectory()
    {
        var defaultFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception("App is installed in an invalid directory");
        if(Application.Current is null || Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new Exception("The current application is not a desktop application.");
        
        var toplevel = TopLevel.GetTopLevel(desktop.MainWindow);
        if (toplevel == null) return;
        var folder = await toplevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            SuggestedStartLocation = await toplevel.StorageProvider.TryGetFolderFromPathAsync(defaultFolder)
        });

        if (folder.Count == 0) return;
        if (!folder[0].Path.IsFile) return;
        var result = _validation(folder[0].Path.LocalPath);
        IsDirectoryValid = result.isValid;
        ErrorMessage = result.errorMessage;
        DisplayError = !IsDirectoryValid;
        if(IsDirectoryValid)
            Result = new DirectoryInfo(folder[0].Path.LocalPath);
        else
            Result = null;
    }
}