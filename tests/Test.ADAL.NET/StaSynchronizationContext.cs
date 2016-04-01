//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

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
