using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using TrebuchetUtils;

namespace Trebuchet.Modals
{
    public class ModlistTextImport : BaseModal
    {
        private bool _append;
        private bool _canceled = true;
        private readonly bool _export;
        private readonly FilePickerFileType _fileType;
        private string _text;

        public ModlistTextImport(string text, bool export, FilePickerFileType fileType) : base(650,600,"Mod List", "ModlistTextImport")
        {
            _text = text;
            _export = export;
            _fileType = fileType;
            CloseDisabled = false;
            MaximizeDisabled = false;
            MinimizeDisabled = false;
            AppendCommand = new SimpleCommand().Subscribe(OnAppend);
            ApplyCommand = new SimpleCommand().Subscribe(OnApply);
            SaveCommand = new SimpleCommand().Subscribe(OnSave);
        }

        public bool Append { get => _append; set => _append = value; }
        public SimpleCommand AppendCommand { get; }
        public SimpleCommand ApplyCommand { get; }
        public bool Canceled { get => _canceled; set => _canceled = value; }
        public bool ImportVisible => !_export;
        public bool SaveAsVisible => _export;
        public SimpleCommand SaveCommand { get; }

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }

        public string Text { get => _text; set => _text = value; }

        private void OnAppend(object? obj)
        {
            if (_export) return;
            _append = true;
            _canceled = false;
            Window.Close();
        }

        private void OnApply(object? obj)
        {
            if (_export) return;
            _canceled = false;
            Window.Close();
        }

        private async void OnSave(object? sender)
        {
            if (!_export) return;
            if (string.IsNullOrEmpty(_text)) return;
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (desktop.MainWindow == null) return;

            var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Save File",
                SuggestedFileName = "Untitled",
                FileTypeChoices = [_fileType]
            });

            if (file is null) return;
            if (!file.Path.IsFile) return;
            var path = Path.GetFullPath(file.Path.LocalPath);
            await File.WriteAllTextAsync(path, _text);
        }
    }
}