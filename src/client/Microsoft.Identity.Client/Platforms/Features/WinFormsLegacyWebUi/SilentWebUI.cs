// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
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
        private const int NavigationOverallTimeout = 10000;

        private SilentWindowsFormsAuthenticationDialog _dialog;
        private bool _disposed;
        private WindowsFormsSynchronizationContext _formsSyncContext;
        private AuthorizationResult _result;
        private ManualResetEvent _threadInitializedEvent;
        private Exception _uiException;

        public SilentWebUI(CoreUIParent parent, RequestContext requestContext)
        {
            OwnerWindow = parent?.OwnerWindow;
            SynchronizationContext = parent?.SynchronizationContext;
            RequestContext = requestContext;
            _threadInitializedEvent = new ManualResetEvent(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SilentWebUI()
        {
            Dispose(false);
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

            bool initialized = _threadInitializedEvent.WaitOne((int)navigationOverallTimeout);
            if (initialized)
            {
                // Calculate time remaining after time spend on initialization.
                // There are 10 000 ticks in each millisecond.
                long elapsedTimeSinceStart = (DateTime.Now.Ticks - navigationStartTime) / 10000;
                navigationOverallTimeout -= elapsedTimeSinceStart;

                bool completedNormally = uiThread.Join(navigationOverallTimeout > 0 ? (int)navigationOverallTimeout : 0);
                if (!completedNormally)
                {
                    RequestContext.Logger.Info("Silent login thread did not complete on time.");

                    // The invisible dialog has failed to complete in the allotted time.
                    // Attempt a graceful shutdown.
                    _formsSyncContext.Post(_ => _dialog.CloseBrowser(), null);
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
                        _formsSyncContext = new WindowsFormsSynchronizationContext();

#pragma warning disable 618 // SilentWindowsFormsAuthenticationDialog is marked obsolete
                        _dialog = new SilentWindowsFormsAuthenticationDialog(OwnerWindow)
                        {
                            NavigationWaitMiliSecs = NavigationWaitMiliSecs,
                            RequestContext = RequestContext
                        };
#pragma warning restore 618

                        _dialog.Done += UIDoneHandler;

                        _threadInitializedEvent.Set();

                        _dialog.AuthenticateAAD(RequestUri, CallbackUri, CancellationToken.None);

                        // Start and turn control over to the message loop.
                        Application.Run();

                        _result = _dialog.Result;
                    }
                    catch (Exception e)
                    {
                        RequestContext.Logger.ErrorPii(e);
                        // Catch all exceptions to transfer them to the original calling thread.
                        _uiException = e;
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
        protected override AuthorizationResult OnAuthenticate(CancellationToken cancellationToken)
        {
            if (null == CallbackUri)
            {
                throw new InvalidOperationException("CallbackUri cannot be null");
            }

            Thread uiSubThread = StartUIThread();

            // Block until the uiSubThread is complete indicating that the invisible dialog has completed
            WaitForCompletionOrTimeout(uiSubThread);
            Cleanup();

            ThrowIfTransferredException();

            if (_result == null)
            {
                throw new MsalUiRequiredException(MsalError.NoPromptFailedError,
                    MsalErrorMessage.NoPromptFailedErrorMessage);
            }

            return _result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_threadInitializedEvent != null)
                    {
                        _threadInitializedEvent.Dispose();
                        _threadInitializedEvent = null;
                    }

                    if (_formsSyncContext != null)
                    {
                        _formsSyncContext.Dispose();
                        _formsSyncContext = null;
                    }
                }

                _disposed = true;
            }
        }

        private void Cleanup()
        {
            _threadInitializedEvent.Dispose();
            _threadInitializedEvent = null;
        }

        private void ThrowIfTransferredException()
        {
            if (null != _uiException)
            {
                throw _uiException;
            }
        }

        private void UIDoneHandler(object sender, SilentWebUIDoneEventArgs e)
        {
            if (_uiException == null)
            {
                _uiException = e.TransferredException;
            }

#pragma warning disable 618 // SilentWindowsFormsAuthenticationDialog is marked obsolete

            // We need call dispose, while message loop is running.
            // WM_QUIT message from ExitThread will delayed, if Dispose will create a set of new messages (we suspect that it happens).
            ((SilentWindowsFormsAuthenticationDialog)sender).Dispose();
            Application.ExitThread();

#pragma warning restore 618
        }
    }
}
