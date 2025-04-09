using System;
using System.IO;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using Trebuchet.Assets;
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

        public ModlistTextImport(string text, bool export, FilePickerFileType fileType) : base(650,600,Resources.ModList, "ModlistTextImport")
        {
            _text = text;
            _export = export;
            _fileType = fileType;
            CloseDisabled = false;
            MaximizeDisabled = false;
            MinimizeDisabled = false;
            AppendCommand = ReactiveCommand.Create(OnAppend);
            ApplyCommand = ReactiveCommand.Create(OnApply);
            SaveCommand = ReactiveCommand.Create(OnSave);
        }

        public bool Append { get => _append; set => _append = value; }
        public ReactiveCommand<Unit, Unit> AppendCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public bool Canceled { get => _canceled; set => _canceled = value; }
        public bool ImportVisible => !_export;
        public bool SaveAsVisible => _export;

        protected override void OnWindowClose(object? sender, EventArgs e)
        {
        }

        public string Text { get => _text; set => _text = value; }

        private void OnAppend()
        {
            if (_export) return;
            _append = true;
            _canceled = false;
            Window.Close();
        }

        private void OnApply()
        {
            if (_export) return;
            _canceled = false;
            Window.Close();
        }

        private async void OnSave()
        {
            if (!_export) return;
            if (string.IsNullOrEmpty(_text)) return;
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (desktop.MainWindow == null) return;

            var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = Resources.SaveFile,
                SuggestedFileName = Resources.Untitled,
                FileTypeChoices = [_fileType]
            });

            if (file is null) return;
            if (!file.Path.IsFile) return;
            var path = Path.GetFullPath(file.Path.LocalPath);
            await File.WriteAllTextAsync(path, _text);
        }
    }
}