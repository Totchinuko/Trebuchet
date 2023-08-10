using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogGUI
{
    public class TaskBlocker : INotifyPropertyChanged
    {
        public const string MainTask = "MainTask";

        private string _description = string.Empty;
        private CancellationTokenSource? _source;
        private Task? _task;
        private Dictionary<string, CancellationTokenSource> _taskSources = new Dictionary<string, CancellationTokenSource>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<string>? TaskSourceChanged;

        public ICommand CancelCommand => new SimpleCommand(OnCancel);

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged("Description");
            }
        }

        public bool IsAvailable => !IsSet(MainTask);

        public void Cancel(string key)
        {
            if (_taskSources.TryGetValue(key, out var source))
                source.Cancel();
        }

        public void CancelMain()
        {
            if (_taskSources.TryGetValue(MainTask, out var source))
                source.Cancel();
        }

        public bool IsSet(string key)
        {
            return _taskSources.ContainsKey(key);
        }

        public void Release(string key)
        {
            if (_taskSources.TryGetValue(key, out var source))
            {
                source.Dispose();
                _taskSources.Remove(key);
                OnTaskSourceChanged(key);
            }
        }

        public void ReleaseMain()
        {
            Description = string.Empty;
            Release(MainTask);
            OnPropertyChanged("IsAvailable");
        }

        public CancellationToken Set(string key, int cancelAfter = 0)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            _taskSources.Add(key, source);
            OnTaskSourceChanged(key);
            if (cancelAfter > 0)
                source.CancelAfter(cancelAfter);
            return source.Token;
        }

        public CancellationToken SetMain(string description, int cancelAfter = 0)
        {
            if (IsSet(MainTask))
                throw new Exception("Cannot set a new blocking task while one is already running");

            Description = description;
            var token = Set(MainTask, cancelAfter);
            OnPropertyChanged("IsAvailable");
            return token;
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void OnTaskSourceChanged(string name)
        {
            TaskSourceChanged?.Invoke(this, name);
        }

        private void OnCancel(object? obj)
        {
            if (!IsSet(MainTask)) return;
            CancelMain();
            Description = "Canceling...";
        }
    }
}