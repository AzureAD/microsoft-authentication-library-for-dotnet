// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_BROKER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    // A simple SynchronizationContext that encapsulates it's own dedicated task queue and processing
    // thread for servicing Send() & Post() calls.  
    // Based upon http://blogs.msdn.com/b/pfxteam/archive/2012/01/20/10259049.aspx but uses it's own thread
    // rather than running on the thread that it's instanciated on
    public sealed class DedicatedThreadSynchronizationContext: SynchronizationContext, IDisposable
    {
        public DedicatedThreadSynchronizationContext()
        {
            m_thread = new Thread(ThreadWorkerDelegate);
            m_thread.Start(this);
        }

        public void Dispose()
        {
            m_queue.CompleteAdding();
        }

        /// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
        /// <param name="d">The System.Threading.SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
                throw new ArgumentNullException("d");
            m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        /// <summary> As 
        public override void Send(SendOrPostCallback d, object state)
        {
            using (var handledEvent = new ManualResetEvent(false))
            {
                Post(SendOrPostCallback_BlockingWrapper, Tuple.Create(d, state, handledEvent));
                handledEvent.WaitOne();
            }
        }

        public int WorkerThreadId { get { return m_thread.ManagedThreadId; } }
        //=========================================================================================

        private static void SendOrPostCallback_BlockingWrapper(object state)
        {
            var innerCallback = (state as Tuple<SendOrPostCallback, object, ManualResetEvent>);
            try
            {
                innerCallback.Item1(innerCallback.Item2);
            }
            finally
            {
                innerCallback.Item3.Set();
            }
        }

        /// <summary>The queue of work items.</summary>
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue =
            new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

        private readonly Thread m_thread = null;

        /// <summary>Runs an loop to process all queued work items.</summary>
        private void ThreadWorkerDelegate(object obj)
        {
            SetSynchronizationContext(obj as SynchronizationContext);

            try
            {
                foreach (var workItem in m_queue.GetConsumingEnumerable())
                    workItem.Key(workItem.Value);
            }
            catch (ObjectDisposedException) { }
        }
    }
}

#endif
