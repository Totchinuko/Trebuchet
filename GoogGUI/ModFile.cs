using Goog;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace GoogGUI
{
    internal class ModFile : INotifyPropertyChanged
    {
        private SteamPublishedFile? _file;
        private FileInfo _infos;
        private bool _isID;
        private bool _isValid;
        private DateTime _lastUpdate;
        private string _mod = string.Empty;

        public ModFile(string mod, string path)
        {
            _mod = mod;
            _isID = long.TryParse(_mod, out _);
            _isValid = _isID || File.Exists(_mod);
            _infos = new FileInfo(path);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsID => _isID;

        public bool IsValid => _isValid;

        public string LastUpdate
        {
            get
            {
                if (!_isID && !_isValid) return string.Empty;
                if(!_isID)
                {
                    DateTime lastModified = _infos.LastWriteTime;
                    return "Last Modified : " + lastModified.ToShortDateString() + " " + lastModified.ToShortTimeString();
                }

                if (_file == null) return "Loading...";
                DateTime local = _lastUpdate.ToLocalTime();
                return "Last Update : " + local.ToShortDateString() + " " + local.ToShortTimeString();
            }
        }

        public string Mod => _mod;

        public string ModName
        {
            get
            {
                if (_file != null)
                    return _file.title;
                if (_isID)
                    return _mod;
                return Path.GetFileName(_mod);
            }
        }

        public SteamPublishedFile? PublishedFile => _file;

        public Brush StatusColor => GetStatusBrush();

        public string StatusTooltip => GetStatusText();

        public void SetManifest(SteamPublishedFile file)
        {
            if (!_isID)
                throw new Exception("Cannot set a manifest on a local mod.");
            _file = file;
            _lastUpdate = Tools.UnixTimeStampToDateTime(_file.timeUpdated);
            OnPropertyChanged("ModName");
            OnPropertyChanged("LastUpdate");
            OnPropertyChanged("StatusColor");
            OnPropertyChanged("StatusTooltip");
        }

        public void RefreshFile()
        {
            _infos.Refresh();
            OnPropertyChanged("StatusColor");
            OnPropertyChanged("StatusTooltip");
        }

        protected virtual Brush GetStatusBrush()
        {
            if (!_infos.Exists) return (Brush)Application.Current.Resources["GDimRed"];
            if (_file == null) return (Brush)Application.Current.Resources["GDimBlue"];
            if (_lastUpdate < _infos.LastWriteTimeUtc) return (Brush)Application.Current.Resources["GDimGreen"];
            return (Brush)Application.Current.Resources["GDimYellow"];
        }

        protected virtual string GetStatusText()
        {
            if (!_infos.Exists) return "Missing";
            if (_file == null) return "Found";
            if (_lastUpdate < _infos.LastWriteTimeUtc) return "Up to Date";
            return "Update available";
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}