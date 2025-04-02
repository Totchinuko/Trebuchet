using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrebuchetUtils;

namespace Trebuchet.Services.TaskBlocker
{
    public sealed class TaskBlocker(ITinyMessengerHub messenger)
    {
        private class BlockedTask(SemaphoreSlim semaphore, CancellationTokenSource cts, IBlockedTaskType type) : IBlockedTask
        {
            public SemaphoreSlim Semaphore { get; } = semaphore;
            public CancellationTokenSource Cts { get; } = cts;
            public IBlockedTaskType Type { get; } = type;
            public event EventHandler<BlockedTask>? OperationReleased;
            public void Release()
            {
                OperationReleased?.Invoke(this, this);
            }
        }
        
        private Dictionary<Type, BlockedTask> _tasks = [];

        public async Task<IBlockedTask> EnterAsync(IBlockedTaskType operation, int cancelAfterSec = 0)
        {
            if (HasCancellableTasks(operation))
                throw new OperationCanceledException();
            if (!_tasks.TryGetValue(operation.GetType(), out BlockedTask? task))
            {
                task = CreateTask(operation, cancelAfterSec);
                task.OperationReleased += (_, t) => Release(t);
            }
            await task.Semaphore.WaitAsync(task.Cts.Token).ConfigureAwait(false);
            _tasks[operation.GetType()] = task;
            OnTaskSourceChanged(operation, true);
            await WaitForBlockingTasks(operation, task.Cts.Token).ConfigureAwait(false);
            return task;
        }
        
        public async Task<IBlockedTask> EnterSingleAsync(IBlockedTaskType operation, int cancelAfterSec = 0)
        {
            if (HasCancellableTasks(operation))
                throw new OperationCanceledException();
            if(_tasks.ContainsKey(operation.GetType()))
                throw new OperationCanceledException();

            var task = CreateTask(operation, cancelAfterSec);
            task.OperationReleased += (_, t) => Release(t);
            await task.Semaphore.WaitAsync(task.Cts.Token).ConfigureAwait(false);
            _tasks[operation.GetType()] = task;
            OnTaskSourceChanged(operation, true);
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

        public LaunchedCommand CreateLaunchedCommand()
        {
            var command = new LaunchedCommand();
            messenger.Subscribe(command);
            return command;
        }

        public TaskBlockedCommand CreateTaskBlockedCommand()
        {
            var command = new TaskBlockedCommand();
            messenger.Subscribe(command);
            return command;
        }

        private BlockedTask CreateTask(IBlockedTaskType operation, int cancelTimer)
        {
            if(cancelTimer > 0)
                return new BlockedTask(
                    new SemaphoreSlim(1, 1), 
                    new CancellationTokenSource(cancelTimer * 1000),
                    operation);
            return new BlockedTask(
                new SemaphoreSlim(1, 1), 
                new CancellationTokenSource(),
                operation);
        }
        
        private void Release(BlockedTask task)
        {
            if (_tasks.TryGetValue(task.Type.GetType(), out var source))
            {
                source.Cts.Dispose();
                source.Semaphore.Release();
                source.Semaphore.Dispose();
                _tasks.Remove(task.Type.GetType());
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
            messenger.Publish(new BlockedTaskStateChanged(type, active));
        }
    }
}