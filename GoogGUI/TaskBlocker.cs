using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace GoogGUI
{
    public class TaskBlocker
    {
        private Dictionary<string, CancellationTokenSource> _taskSources = new Dictionary<string, CancellationTokenSource>();

        public event EventHandler<string>? TaskSourceChanged;

        public void Cancel(string key)
        {
            if (_taskSources.TryGetValue(key, out var source))
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

        public CancellationTokenSource Set(string key, int cancelAfter = 0)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            _taskSources.Add(key, cts);
            OnTaskSourceChanged(key);
            if (cancelAfter > 0)
                cts.CancelAfter(cancelAfter);
            return cts;
        }

        protected virtual void OnTaskSourceChanged(string name)
        {
            TaskSourceChanged?.Invoke(this, name);
        }
    }
}