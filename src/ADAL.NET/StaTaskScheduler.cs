//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

// This file was copied from .NET Framework samples at http://code.msdn.microsoft.com/ParExtSamples

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Moving the code to the library's main namespace
namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    // This IDisposable class doe not need to implement Dispose method in standard way, because it is sealed. 
    // If it ever needs to become inheritable, it should follow the standard pattern as desribed in http://msdn.microsoft.com/en-us/library/fs2xkftw(v=vs.110).aspx.
    /// <summary>Provides a scheduler that uses STA threads.</summary>
    internal sealed class StaTaskScheduler : TaskScheduler, IDisposable
    {
        /// <summary>Stores the queued tasks to be executed by our pool of STA threads.</summary>
        private BlockingCollection<Task> _tasks;
        /// <summary>The STA threads used by the scheduler.</summary>
        private readonly List<Thread> _threads;

        /// <summary>Initializes a new instance of the StaTaskScheduler class with the specified concurrency level.</summary>
        /// <param name="numberOfThreads">The number of threads that should be created and used by this scheduler.</param>
        public StaTaskScheduler(int numberOfThreads)
        {
            // Validate arguments
            if (numberOfThreads < 1)
            {
                throw new ArgumentOutOfRangeException("numberOfThreads");
            }

            // Initialize the tasks collection
            _tasks = new BlockingCollection<Task>();

            // Create the threads to be used by this scheduler
            _threads = Enumerable.Range(0, numberOfThreads).Select(i =>
                       {
                           var thread = new Thread(() =>
                           {
                               // Continually get the next task and try to execute it.
                               // This will continue until the scheduler is disposed and no more tasks remain.
                               foreach (var t in _tasks.GetConsumingEnumerable())
                               {
                                   TryExecuteTask(t);
                               }
                           }) { IsBackground = true };
                           thread.SetApartmentState(ApartmentState.STA);
                           return thread;
                       }).ToList();

            // Start all of the threads
            _threads.ForEach(t => t.Start());
        }

        /// <summary>Queues a Task to be executed by this scheduler.</summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task)
        {
            // Push it into the blocking collection of tasks
            _tasks.Add(task);
        }

        /// <summary>Provides a list of the scheduled tasks for the debugger to consume.</summary>
        /// <returns>An enumerable of all tasks currently scheduled.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Serialize the contents of the blocking collection of tasks for the debugger
            return _tasks.ToArray();
        }

        /// <summary>Determines whether a Task may be inlined.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
        /// <returns>true if the task was successfully inlined; otherwise, false.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // Try to inline if the current thread is STA
            return
                Thread.CurrentThread.GetApartmentState() == ApartmentState.STA &&
                TryExecuteTask(task);
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel
        {
            get { return _threads.Count; }
        }

        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            if (_tasks != null)
            {
                // Indicate that no new tasks will be coming in
                _tasks.CompleteAdding();

                // Wait for all threads to finish processing tasks
                foreach (var thread in _threads) thread.Join();

                // Cleanup
                _tasks.Dispose();
                _tasks = null;
            }
        }
    }
}