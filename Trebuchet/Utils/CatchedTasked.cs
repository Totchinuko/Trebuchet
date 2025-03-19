using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;
using TrebuchetUtils.Modals;

namespace Trebuchet.Utils
{
    public class CatchedTasked
    {
        private int _cancelAfter;
        private Operations _operations;
        private BlockingCollection<Func<CancellationTokenSource, Task>> _tasks = new BlockingCollection<Func<CancellationTokenSource, Task>>();
        private BlockingCollection<Action> _then = new BlockingCollection<Action>();

        public CatchedTasked(Operations operations, int cancelAfter = 0)
        {
            _operations = operations;
            _cancelAfter = cancelAfter;
        }

        public CatchedTasked Add(Func<CancellationTokenSource, Task> task)
        {
            _tasks.Add(task);
            return this;
        }

        public async void Start()
        {
            _tasks.CompleteAdding();
            _then.CompleteAdding();
            var cts = StrongReferenceMessenger.Default.Send(new OperationStartMessage(_operations, _cancelAfter)).Response;

            try
            {
                foreach (var task in _tasks.GetConsumingEnumerable())
                {
                    await task(cts);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Information("Operation cancelled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CatchedTasked failed to complete");
                await new ErrorModal("Error", $"{ex.Message + Environment.NewLine}Please check the log for more information.").OpenDialogueAsync();
                return;
            }
            finally
            {
                StrongReferenceMessenger.Default.Send(new OperationReleaseMessage(_operations));
            }
            foreach (var action in _then.GetConsumingEnumerable())
            {
                action();
            }

            _then.Dispose();
            _tasks.Dispose();
        }

        public CatchedTasked Then(Action action)
        {
            _then.Add(action);
            return this;
        }
    }
}