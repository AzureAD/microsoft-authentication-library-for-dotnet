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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    [ComVisible(true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SilentWindowsFormsAuthenticationDialog : WindowsFormsWebAuthenticationDialogBase
    {
        internal delegate void SilentWebUIDoneEventHandler(object sender, SilentWebUIDoneEventArgs args);
        internal event SilentWebUIDoneEventHandler Done;

        private DateTime navigationExpiry = DateTime.MaxValue;

        private Timer timer;

        private bool doneSignaled;

        public int NavigationWaitMiliSecs { get; set; }

        public SilentWindowsFormsAuthenticationDialog(object ownerWindow)
            : base(ownerWindow)
        {
            this.SuppressBrowserSubDialogs();
            this.WebBrowser.DocumentCompleted += this.DocumentCompletedHandler;
        }

        public void CloseBrowser()
        {
            this.SignalDone();
        }

        public string Result
        {
            get
            {
                return this.authenticationResult;
            }
        }

        /// <summary>
        /// Make sure that the browser control does not surface any of it's own dialogs.
        /// For instance bad certificate or javascript error dialogs.
        /// </summary>
        private void SuppressBrowserSubDialogs()
        {
            var webBrowser2 = (NativeWrapper.IWebBrowser2)this.WebBrowser.ActiveXInstance;
            webBrowser2.Silent = true;
        }

        protected override void WebBrowserNavigatingHandler(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (null == timer)
            {
                this.timer = CreateStartedTimer(
                    () =>
                    {
                        DateTime now = DateTime.Now;
                        if (now > this.navigationExpiry)
                        {
                            this.OnUserInteractionRequired();
                        }
                    },
                    this.NavigationWaitMiliSecs);
            }

            // We don't timeout each individual navigation, only the time between individual navigations.
            // Reset the expiry time so that it isn't relevant until the next document complete.
            this.navigationExpiry = DateTime.MaxValue;

            base.WebBrowserNavigatingHandler(sender, e);
        }

        private static Timer CreateStartedTimer(Action onTickAction, int interval)
        {
            Timer timer = new Timer { Interval = interval };
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
            if (!this.doneSignaled)
            {
                this.timer.Stop();
                SilentWebUIDoneEventArgs args = new SilentWebUIDoneEventArgs(exception);

                if (null != this.Done)
                {
                    this.Done(this, args);
                }

                this.doneSignaled = true;
            }
        }

        private void DocumentCompletedHandler(object sender, WebBrowserDocumentCompletedEventArgs args)
        {
            this.navigationExpiry = DateTime.Now.AddMilliseconds(NavigationWaitMiliSecs);
            if (this.HasLoginPage())
            {
                this.OnUserInteractionRequired();
            }
        }

        private void OnUserInteractionRequired()
        {
            this.SignalDone(
                new AdalException(AdalError.UserInteractionRequired));
        }

        protected override void OnClosingUrl()
        {
            this.SignalDone();
        }

        protected override void OnNavigationCanceled(int statusCode)
        {
            this.SignalDone(this.CreateExceptionForAuthenticationUiFailed(statusCode));
        }

        private bool HasLoginPage()
        {
            HtmlDocument doc = this.WebBrowser.Document;
            HtmlElement passwordFieldElement = null;

            if (null != doc)
            {
                passwordFieldElement =
                    (
                        from element in doc.GetElementsByTagName("INPUT").Cast<HtmlElement>()
                        where
                               0 == String.Compare(element.GetAttribute("type"), "password", true, CultureInfo.InvariantCulture)
                            && element.Enabled
                            && element.OffsetRectangle.Height > 0
                            && element.OffsetRectangle.Width > 0
                        select element
                    ).FirstOrDefault();
            }

            return passwordFieldElement != null;
        }

        protected override void Dispose(bool disposing)
        {
            if (this.timer != null)
            {
                this.timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
