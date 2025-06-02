// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Utils
{
    internal struct MainThreadActionItem
    {
        public Action Action { get; }
        public TaskCompletionSource<bool> Completion { get; }
        public bool IsAsyncAction { get; }

        public MainThreadActionItem(Action action, TaskCompletionSource<bool> completion, bool isAsyncAction)
        {
            Action = action;
            Completion = completion;
            IsAsyncAction = isAsyncAction;
        }
    }

    /// <summary>
    /// This class implements a main thread scheduler for macOS applications. It should be also working on other platforms, but it is primarily designed for macOS.
    /// It is mainly designed for internal use to support the MSAL library in "switching to the main thread anytime". 
    /// However, external users can also call it if needed.
    /// </summary>
    public class MacMainThreadScheduler
    {
        private readonly ConcurrentQueue<MainThreadActionItem> _mainThreadActions;

        private volatile bool _workerFinished;
        private volatile bool _isRunning;

        // Configurable sleep time in milliseconds in the message loop.
        private const int WorkerSleepInMilliseconds = 10;

        // Singleton mode
        private static readonly Lazy<MacMainThreadScheduler> _instance = new Lazy<MacMainThreadScheduler>(() => new MacMainThreadScheduler());

        /// <summary>
        /// Gets the singleton instance of MacMainThreadScheduler
        /// </summary>
        public static MacMainThreadScheduler Instance()
        {
            return _instance.Value;
        }

        /// <summary>
        /// Private constructor for MacMainThreadScheduler (singleton pattern)
        /// </summary>
        private MacMainThreadScheduler()
        {
            _mainThreadActions = new ConcurrentQueue<MainThreadActionItem>();
            _workerFinished = false;
            _isRunning = false;
        }

        /// <summary>
        /// Check if the current thread is the main thread.
        /// </summary>
        public bool IsCurrentlyOnMainThread()
        {
            return Environment.CurrentManagedThreadId == 1; // Main thread id is always 1 on macOS.
        }
        
        /// <summary>
        /// Check if the message loop is currently running.
        /// </summary>
        public bool IsRunning()
        {
            return _isRunning;
        }
        
        /// <summary>
        /// Stop the main thread message loop
        /// </summary>
        public void Stop()
        {
            _workerFinished = true;
        }

        /// <summary>
        /// Run on the main thread asynchronously.
        /// </summary>
        /// <param name="asyncAction">action</param>
        /// <returns>FinishedTask</returns>
        public Task RunOnMainThreadAsync(Func<Task> asyncAction)
        {
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));

            var tcs = new TaskCompletionSource<bool>();
            Action wrapper = () =>
            {
                try
                {
                    asyncAction().ContinueWith(task =>
                    {
                        if (task.IsFaulted && task.Exception != null)
                        {
                            tcs.TrySetException(task.Exception.InnerExceptions);
                        }
                        else
                        {
                            tcs.TrySetResult(true);
                        }
                    }, TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            _mainThreadActions.Enqueue(new MainThreadActionItem(wrapper, tcs, true));
            return tcs.Task;
        }

        /// <summary>
        /// Start the message loop on the main thread to process actions
        /// </summary>
        public void StartMessageLoop()
        {
            if (!IsCurrentlyOnMainThread())
                throw new InvalidOperationException("Message loop must be started on the main thread.");

            if (_isRunning)
                throw new InvalidOperationException("StartMessageLoop already running.");

            _isRunning = true;
            _workerFinished = false;
            try
            {
                while (!_workerFinished)
                {
                    while (_mainThreadActions.TryDequeue(out var actionItem))
                    {
                        try
                        {
                            actionItem.Action();
                            if (!actionItem.IsAsyncAction)
                            {
                                actionItem.Completion.TrySetResult(true);
                            }
                        }
                        catch (Exception ex)
                        {
                            actionItem.Completion.TrySetException(ex);
                        }
                    }
                    // Sleep for a short interval to avoid busy-waiting and reduce CPU usage while waiting for new actions in the queue.
                    Thread.Sleep(WorkerSleepInMilliseconds);
                }
            }
            finally
            {
                _isRunning = false;
            }
        }

    }

}
