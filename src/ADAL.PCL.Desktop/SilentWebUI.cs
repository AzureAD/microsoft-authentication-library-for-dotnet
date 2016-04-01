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
using System.Windows.Forms;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    internal class SilentWebUI : WebUI, IDisposable
    {
        /// <summary>
        /// This is how long we allow between completed navigations.
        /// </summary>
        private const int NavigationWaitMiliSecs = 250;

        /// <summary>
        /// This is how long all redirect navigations are allowed to run for before a graceful 
        /// termination of the entire browser based authentication process is attempted.
        /// </summary>
        private const int NavigationOverallTimeout = 2000;

        private bool disposed;

        private WindowsFormsSynchronizationContext formsSyncContext;

        private AuthorizationResult result;

        private Exception uiException;

        private ManualResetEvent threadInitializedEvent;

        private SilentWindowsFormsAuthenticationDialog dialog;

        public SilentWebUI()
        {
            this.threadInitializedEvent = new ManualResetEvent(false);
        }

        ~SilentWebUI()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Waits on the UI Thread to complete normally for NavigationOverallTimeout.  
        /// After it attempts shutdown the UI thread graceful followed by aborting
        /// the thread if a graceful shutdown is not successful.
        /// </summary>
        /// <param name="uiThread"></param>
        /// <returns>Returns true if the UI thread completed on its own before the timeout.  Otherwise false.</returns>
        private void WaitForCompletionOrTimeout(Thread uiThread)
        {
            long navigationOverallTimeout = NavigationOverallTimeout;
            long navigationStartTime = DateTime.Now.Ticks;

            bool initialized = this.threadInitializedEvent.WaitOne((int)navigationOverallTimeout);
            if (initialized)
            {
                // Calculate time remaining after time spend on initialization.
                // There are 10 000 ticks in each millisecond.
                long elapsedTimeSinceStart = (DateTime.Now.Ticks - navigationStartTime) / 10000;
                navigationOverallTimeout -= elapsedTimeSinceStart;

                bool completedNormally = uiThread.Join(navigationOverallTimeout > 0 ? (int)navigationOverallTimeout : 0);
                if (!completedNormally)
                {
                    PlatformPlugin.Logger.Information(null, "Silent login thread did not complete on time.");

                    // The invisible dialog has failed to complete in the allotted time.
                    // Attempt a graceful shutdown.
                    this.formsSyncContext.Post(state => this.dialog.CloseBrowser(), null);
                }
            }
        }

        private Thread StartUIThread()
        {
            // Start a new UI thread to run the browser dialog on so that we can block this one and present
            // a synchronous interface to callers.
            Thread uiSubThread = new Thread(
                () =>
                {
                    try
                    {
                        this.formsSyncContext = new WindowsFormsSynchronizationContext();

                        this.dialog = new SilentWindowsFormsAuthenticationDialog(this.OwnerWindow)
                        {
                            NavigationWaitMiliSecs = NavigationWaitMiliSecs
                        };

                        this.dialog.Done += this.UIDoneHandler;

                        this.threadInitializedEvent.Set();

                        this.dialog.AuthenticateAAD(this.RequestUri, this.CallbackUri);

                        // Start and turn control over to the message loop.
                        Application.Run();

                        this.result = this.dialog.Result;
                    }
                    catch (Exception e)
                    {
                        // Catch all exceptions to transfer them to the original calling thread.
                        this.uiException = e;
                    }
                });

            uiSubThread.SetApartmentState(ApartmentState.STA);
            uiSubThread.IsBackground = true;
            uiSubThread.Start();

            return uiSubThread;
        }

        /// <summary>
        /// Callers expect the call to show the authentication dialog to be synchronous.  This is easy in the 
        /// interactive case as ShowDialog is a synchronous call.  However, ShowDialog will always show 
        /// the dialog.  It can not be hidden. So it can not be used in the silent case.  Instead we need
        /// to do the equivalent of creating our own modal dialog.  We start a new thread, launch an 
        /// invisible window on that thread.  The original calling thread blocks until the secondary
        /// UI thread completes.  
        /// </summary>
        /// <returns></returns>
        protected override AuthorizationResult OnAuthenticate()
        {
            if (null == this.CallbackUri)
            {
                throw new InvalidOperationException("CallbackUri cannot be null");
            }

            Thread uiSubThread = this.StartUIThread();

            // Block until the uiSubThread is complete indicating that the invisible dialog has completed
            this.WaitForCompletionOrTimeout(uiSubThread);
            this.Cleanup();

            this.ThrowIfTransferredException();

            if (this.result == null)
            {
                throw new AdalException(AdalError.UserInteractionRequired);
            }

            return this.result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.threadInitializedEvent != null)
                    {
                        this.threadInitializedEvent.Dispose();
                        this.threadInitializedEvent = null;
                    }

                    if (this.formsSyncContext != null)
                    {
                        this.formsSyncContext.Dispose();
                        this.formsSyncContext = null;                        
                    }
                }

                disposed = true;
            }
        }

        private void Cleanup()
        {
            this.threadInitializedEvent.Dispose();
            this.threadInitializedEvent = null;
        }

        private void ThrowIfTransferredException()
        {
            if (null != this.uiException)
            {
                throw this.uiException;
            }
        }

        private void UIDoneHandler(object sender, SilentWebUIDoneEventArgs e)
        {
            if (this.uiException == null)
            {
                this.uiException = e.TransferedException;
            }

            // We need call dispose, while message loop is running.
            // WM_QUIT message from ExitThread will delayed, if Dispose will create a set of new messages (we suspect that it happens).
            ((SilentWindowsFormsAuthenticationDialog)sender).Dispose();
            Application.ExitThread();
        }
    }
}
