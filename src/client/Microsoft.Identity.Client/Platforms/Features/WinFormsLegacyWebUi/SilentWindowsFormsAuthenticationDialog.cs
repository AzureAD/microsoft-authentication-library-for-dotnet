// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    /// <summary>
    /// </summary>
    [ComVisible(true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class SilentWindowsFormsAuthenticationDialog : WindowsFormsWebAuthenticationDialogBase
    {
        private bool doneSignaled;
        private DateTime navigationExpiry = DateTime.MaxValue;
        private Timer timer;

        /// <summary>
        /// </summary>
        public SilentWindowsFormsAuthenticationDialog(object ownerWindow)
            : base(ownerWindow)
        {
            SuppressBrowserSubDialogs();
            WebBrowser.DocumentCompleted += DocumentCompletedHandler;
        }

        /// <summary>
        /// </summary>
        public int NavigationWaitMiliSecs { get; set; }

        internal event SilentWebUIDoneEventHandler Done;

        /// <summary>
        /// </summary>
        public void CloseBrowser()
        {
            SignalDone();
        }

        /// <summary>
        /// Make sure that the browser control does not surface any of it's own dialogs.
        /// For instance bad certificate or javascript error dialogs.
        /// </summary>
        private void SuppressBrowserSubDialogs()
        {
            var webBrowser2 = (NativeWrapper.IWebBrowser2)WebBrowser.ActiveXInstance;
            webBrowser2.Silent = true;
        }

        /// <summary>
        /// </summary>
        protected override void WebBrowserBeforeNavigateHandler(object sender, WebBrowserBeforeNavigateEventArgs e)
        {
            if (null == timer)
            {
                timer = CreateStartedTimer(
                    () =>
                    {
                        DateTime now = DateTime.Now;
                        if (now > navigationExpiry)
                        {
                            OnUserInteractionRequired();
                        }
                    },
                    NavigationWaitMiliSecs);
            }

            // We don't timeout each individual navigation, only the time between individual navigations.
            // Reset the expiry time so that it isn't relevant until the next document complete.
            navigationExpiry = DateTime.MaxValue;

            base.WebBrowserBeforeNavigateHandler(sender, e);
        }

        private static Timer CreateStartedTimer(Action onTickAction, int interval)
        {
            Timer timer = new Timer { Interval = interval };
            timer.Tick += (_, _) => onTickAction();
            timer.Start();
            return timer;
        }

        /// <summary>
        /// This method must only be called from the UI thread.  Since this is the
        /// callers opportunity to call dispose on this object.  Calling
        /// Dispose must be done on the same thread on which this object
        /// was constructed.
        /// </summary>
        /// <param name="exception"></param>
        private void SignalDone(Exception exception = null)
        {
            if (!doneSignaled)
            {
                timer.Stop();
                SilentWebUIDoneEventArgs args = new SilentWebUIDoneEventArgs(exception);

                Done?.Invoke(this, args);

                doneSignaled = true;
            }
        }

        private void DocumentCompletedHandler(object sender, WebBrowserDocumentCompletedEventArgs args)
        {
            navigationExpiry = DateTime.Now.AddMilliseconds(NavigationWaitMiliSecs);
            if (HasLoginPage())
            {
                OnUserInteractionRequired();
            }
        }

        private void OnUserInteractionRequired()
        {
            SignalDone(
                new MsalUiRequiredException(MsalError.NoPromptFailedError, MsalErrorMessage.NoPromptFailedErrorMessage));
        }

        /// <summary>
        /// </summary>
        protected override void OnClosingUrl()
        {
            SignalDone();
        }

        /// <summary>
        /// </summary>
        protected override void OnNavigationCanceled(int statusCode)
        {
            SignalDone(CreateExceptionForAuthenticationUiFailed(statusCode));
        }

        private bool HasLoginPage()
        {
            HtmlDocument doc = WebBrowser.Document;
            HtmlElement passwordFieldElement = null;

            if (null != doc)
            {
                passwordFieldElement =
                    (
                        from element in doc.GetElementsByTagName("INPUT").Cast<HtmlElement>()
                        where
                            0 == string.Compare(element.GetAttribute("type"), "password", StringComparison.Ordinal)
                            && element.Enabled
                            && element.OffsetRectangle.Height > 0
                            && element.OffsetRectangle.Width > 0
                        select element
                        ).FirstOrDefault();
            }

            return passwordFieldElement != null;
        }

        /// <summary>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (timer != null)
            {
                timer.Dispose();
            }
            base.Dispose(disposing);
        }

        internal delegate void SilentWebUIDoneEventHandler(object sender, SilentWebUIDoneEventArgs args);
    }
}
