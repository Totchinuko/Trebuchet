using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrebuchetUtils;

namespace Trebuchet.Services.TaskBlocker
{
    public sealed class TaskBlocker
    {
        private class BlockedTask(SemaphoreSlim semaphore, CancellationTokenSource cts, IBlockedTaskType type) : IBlockedTask
        {
            public SemaphoreSlim Semaphore => semaphore;
            public CancellationTokenSource Cts => cts;
            public IBlockedTaskType Type => type;
            public event EventHandler? OperationReleased;
            public void Release()
            {
                OperationReleased?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private Dictionary<Type, BlockedTask> _tasks = [];

        public async Task<IBlockedTask> EnterAsync<T>(T operation, int cancelAfterSec = 0) where T : IBlockedTaskType
        {
            if (HasCancellableTasks(operation))
                throw new OperationCanceledException();
            if (!_tasks.TryGetValue(operation.GetType(), out BlockedTask? task))
            {
                task = new BlockedTask(new SemaphoreSlim(1, 1), new CancellationTokenSource(cancelAfterSec * 1000), operation);
                task.OperationReleased += (_, _) => Release<T>();
            }
            await task.Semaphore.WaitAsync(task.Cts.Token).ConfigureAwait(false);
            _tasks[operation.GetType()] = task;
            await WaitForBlockingTasks(operation, task.Cts.Token).ConfigureAwait(false);
            return task;
        }
        
        public async Task<IBlockedTask> EnterSingleAsync<T>(T operation, int cancelAfterSec = 0) where T : IBlockedTaskType
        {
            if (HasCancellableTasks(operation))
                throw new OperationCanceledException();
            if(_tasks.ContainsKey(operation.GetType()))
                throw new OperationCanceledException();
            
            var task = new BlockedTask(new SemaphoreSlim(1, 1), new CancellationTokenSource(cancelAfterSec * 1000), operation);
            task.OperationReleased += (_, _) => Release<T>();
            await task.Semaphore.WaitAsync(task.Cts.Token).ConfigureAwait(false);
            _tasks[operation.GetType()] = task;
            await WaitForBlockingTasks(operation, task.Cts.Token).ConfigureAwait(false);
            return task;
        }

        public void Cancel<T>() where T : IBlockedTaskType
        {
            if (_tasks.TryGetValue(typeof(T), out var source))
            {
                source.Cts.Cancel();
            }
        }

        public bool IsSet<T>() where T : IBlockedTaskType
        {
            return _tasks.ContainsKey(typeof(T));
        }
        
        private void Release<T>() where T : IBlockedTaskType
        {
            if (_tasks.TryGetValue(typeof(T), out var source))
            {
                source.Cts.Dispose();
                source.Semaphore.Release();
                source.Semaphore.Dispose();
                _tasks.Remove(typeof(T));
                OnTaskSourceChanged(source.Type, false);
            }
        }

        private bool HasCancellableTasks(IBlockedTaskType type)
        {
            return type.CancellingTypes.Any(cType => _tasks.ContainsKey(cType));
        }

        private async Task WaitForBlockingTasks(IBlockedTaskType type, CancellationToken token)
        {
            foreach(var blockingType in type.BlockingTypes)
                if (_tasks.TryGetValue(blockingType, out var task))
                    await Task.Run(() => task.Semaphore.AvailableWaitHandle.WaitOne(), token);
        }

        private void OnTaskSourceChanged(IBlockedTaskType type, bool active)
        {
            TinyMessengerHub.Default.Publish(new BlockedTaskStateChanged(type, active));
        }
    }
}