using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace GoogGUI
{
    public class TaskBlocker : INotifyPropertyChanged
    {
        public const string MainTask = "MainTask";

        private string _description = string.Empty;
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

        public bool IsSet(params string[] key)
        {
            return key.Any(_taskSources.ContainsKey);
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

        public CancellationTokenSource Set(string key, int cancelAfter = 0)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            _taskSources.Add(key, cts);
            OnTaskSourceChanged(key);
            if (cancelAfter > 0)
                cts.CancelAfter(cancelAfter);
            return cts;
        }

        public CancellationTokenSource SetMain(string description, int cancelAfter = 0)
        {
            if (IsSet(MainTask))
                throw new Exception("Cannot set a new blocking task while one is already running");

            Description = description;
            var cts = Set(MainTask, cancelAfter);
            OnPropertyChanged("IsAvailable");
            return cts;
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