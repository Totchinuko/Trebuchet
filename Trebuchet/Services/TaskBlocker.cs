using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Messages;
using TrebuchetLib;

namespace Trebuchet.Services
{
    public class TaskBlocker
    {
        private Dictionary<Operations, CancellationTokenSource> _taskSources = [];

        public TaskBlocker()
        {
        }

        public void Cancel(Operations operation)
        {
            if (_taskSources.TryGetValue(operation, out var source))
                source.Cancel();
        }

        public bool IsSet(params Operations[] operation)
        {
            return operation.Any(_taskSources.ContainsKey);
        }
        
        public void Release(Operations operation)
        {
            if (_taskSources.TryGetValue(operation, out var source))
            {
                source.Dispose();
                _taskSources.Remove(operation);
                OnTaskSourceChanged(operation);
            }
        }

        public CancellationTokenSource Set(Operations operation, int cancelAfter = 0)
        {
            if(IsSet(operation))
                throw new TrebException("Operation is already running");
            
            CancellationTokenSource cts = new CancellationTokenSource();
            _taskSources.Add(operation, cts);
            OnTaskSourceChanged(operation);
            if (cancelAfter > 0)
                cts.CancelAfter(cancelAfter);
            return cts;
        }

        protected virtual void OnTaskSourceChanged(Operations operation)
        {
            StrongReferenceMessenger.Default.Send(new OperationStateChanged(operation, IsSet(operation)));
        }
    }
}