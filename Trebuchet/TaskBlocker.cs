using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Trebuchet
{
    internal class TaskBlocker : IRecipient<OperationMessage>,
        IRecipient<OperationStartMessage>,
        IRecipient<OperationStateRequest>
    {
        private Dictionary<Operations, CancellationTokenSource> _taskSources = new Dictionary<Operations, CancellationTokenSource>();

        public TaskBlocker()
        {
            StrongReferenceMessenger.Default.Register<OperationReleaseMessage>(this);
            StrongReferenceMessenger.Default.Register<OperationCancelMessage>(this);
            StrongReferenceMessenger.Default.Register<OperationStartMessage>(this);
            StrongReferenceMessenger.Default.Register<OperationStateRequest>(this);
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

        public void Receive(OperationMessage message)
        {
            if (message is OperationCancelMessage cancelMessage)
                Cancel(cancelMessage.key);
            else if (message is OperationReleaseMessage releaseMessage)
                Release(releaseMessage.key);
        }

        public void Receive(OperationStartMessage message)
        {
            message.Reply(Set(message.key, message.cancelAfter));
        }

        public void Receive(OperationStateRequest message)
        {
            message.Reply(IsSet(message.keys));
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