﻿using CommunityToolkit.Mvvm.Messaging;
using SteamKit2.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;

namespace Trebuchet.Utils
{
    public class CatchedTasked
    {
        private Operations _operations;
        private BlockingCollection<Func<CancellationTokenSource, Task>> _tasks = new BlockingCollection<Func<CancellationTokenSource, Task>>();
        private BlockingCollection<Action> _then = new BlockingCollection<Action>();

        public CatchedTasked(Operations operations)
        {
            _operations = operations;
        }

        public CatchedTasked Add(Func<CancellationTokenSource, Task> task)
        {
            _tasks.Add(task);
            return this;
        }

        public void Start()
        {
            _tasks.CompleteAdding();
            _then.CompleteAdding();
            var cts = StrongReferenceMessenger.Default.Send(new OperationStartMessage(_operations)).Response;

            Task.Run(async () =>
            {
                try
                {
                    foreach (var task in _tasks.GetConsumingEnumerable())
                    {
                        await task(cts);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("Error", $"{ex.Message + Environment.NewLine}Please check the log for more information.").ShowDialog();
                        return;
                    });
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StrongReferenceMessenger.Default.Send(new OperationReleaseMessage(_operations));
                    });
                }
                foreach (var action in _then.GetConsumingEnumerable())
                {
                    action();
                }
            }, cts.Token);

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