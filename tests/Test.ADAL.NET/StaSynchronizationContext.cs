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

using System;
using System.Threading;
using System.Windows.Threading;

namespace Test.ADAL.NET
{
    public class StaSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly ManualResetEvent dispatcherEvent = new ManualResetEvent(false);
        private Dispatcher dispatcher;

        public StaSynchronizationContext()
        {
            Thread thread = new Thread(this.Start) { IsBackground = false, Name = "STA Thread" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // Wait for dispatcher to be set
            this.dispatcherEvent.WaitOne();
            SynchronizationContext.SetSynchronizationContext(this);
        }

        ~StaSynchronizationContext()
        {
            this.Dispose();
        }

        public override void Post(SendOrPostCallback method, object state)
        {
            this.dispatcher.BeginInvoke(method, new[] { state });
        }

        public override void Send(SendOrPostCallback method, object state)
        {
            this.dispatcher.Invoke(method, new[] { state });
        }

        public void Dispose()
        {
            if (!this.dispatcher.HasShutdownStarted && !this.dispatcher.HasShutdownFinished)
            {
                this.dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            }
        }

        private void Start(object param)
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.dispatcherEvent.Set();
            Dispatcher.Run();
        }
    }
}
