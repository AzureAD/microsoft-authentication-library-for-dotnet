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
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    [ComVisible(true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SilentWindowsFormsAuthenticationDialog : WindowsFormsWebAuthenticationDialogBase
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
            WebBrowser.DocumentCompleted += this.DocumentCompletedHandler;
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
            var webBrowser2 = (NativeWrapper.IWebBrowser2) WebBrowser.ActiveXInstance;
            webBrowser2.Silent = true;
        }

        /// <summary>
        /// </summary>
        protected override void WebBrowserNavigatingHandler(object sender, WebBrowserNavigatingEventArgs e)
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

            base.WebBrowserNavigatingHandler(sender, e);
        }

        private static Timer CreateStartedTimer(Action onTickAction, int interval)
        {
            Timer timer = new Timer {Interval = interval};
            timer.Tick += (notUsedsender, notUsedEventArgs) => onTickAction();
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

                if (null != Done)
                {
                    Done(this, args);
                }

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
                new MsalException(MsalError.UserInteractionRequired));
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
                            0 == String.Compare(element.GetAttribute("type"), "password", StringComparison.Ordinal)
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