using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using TrebuchetUtils;

namespace Trebuchet.Services.TaskBlocker
{
    public sealed class TaskChangedEventArgs(IBlockedTaskType type, bool toggle, IEnumerable<Type> actives) : EventArgs
    {
        public IBlockedTaskType Type { get; } = type;
        public bool Toggle { get; } = toggle;
        public IEnumerable<Type> ActiveTypes { get; } = actives;
    }
    
    public sealed class TaskBlocker : ReactiveObject
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
        private readonly ITinyMessengerHub _messenger;
        private event EventHandler<TaskChangedEventArgs>? TaskChanged;
        private ObservableAsPropertyHelper<bool> _canDownloadMods;
        private ObservableAsPropertyHelper<bool> _canDownloadServers;
        private ObservableAsPropertyHelper<bool> _canLaunch;

        public TaskBlocker(ITinyMessengerHub messenger)
        {
            _messenger = messenger;
            TaskChanges = Observable.FromEventPattern<TaskChangedEventArgs>(
                handler => TaskChanged += handler,
                hander => TaskChanged -= hander);

            Type[] downloadMods = [typeof(SteamDownload), typeof(ServersRunning), typeof(ClientRunning)];
            Type[] downloadServers = [typeof(SteamDownload), typeof(ServersRunning)];
            _canDownloadMods = TaskChanges.Select(x => !x.EventArgs.ActiveTypes.Intersect(downloadMods).Any())
                .StartWith(true)
                .ToProperty(this, x => x.CanDownloadMods);
            _canDownloadServers = TaskChanges.Select(x => !x.EventArgs.ActiveTypes.Intersect(downloadServers).Any())
                .StartWith(true)
                .ToProperty(this, x => x.CanDownloadServer);
            _canLaunch = TaskChanges.Select(x => !x.EventArgs.ActiveTypes.Contains(typeof(SteamDownload)))
                .StartWith(true)
                .ToProperty(this, x => x.CanLaunch);
        }

        public IObservable<EventPattern<TaskChangedEventArgs>> TaskChanges { get; }

        public bool CanDownloadMods => _canDownloadMods.Value;
        public bool CanDownloadServer => _canDownloadServers.Value;
        public bool CanLaunch => _canLaunch.Value;

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
            OnTaskChanged(operation, true);
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
            OnTaskChanged(operation, true);
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

        public bool IsSet(params Type[] types)
        {
            return types.Any(type => _tasks.ContainsKey(type));
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
                OnTaskChanged(task.Type, false);
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
            _messenger.Publish(new BlockedTaskStateChanged(type, active));
        }

        private void OnTaskChanged(IBlockedTaskType t, bool toggled)
        {
            TaskChanged?.Invoke(this, new TaskChangedEventArgs(t, toggled, _tasks.Keys));
        }
    }
}