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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    /// <summary>
    /// The browser dialog used for user authentication
    /// </summary>
    [ComVisible(true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class WindowsFormsWebAuthenticationDialog : WindowsFormsWebAuthenticationDialogBase
    {
        private bool zoomed;

        private int statusCode;

        /// <summary>
        /// Default constructor
        /// </summary>
        public WindowsFormsWebAuthenticationDialog(object ownerWindow)
            : base(ownerWindow)
        {
            this.Shown += this.FormShownHandler;
            this.WebBrowser.DocumentTitleChanged += this.WebBrowserDocumentTitleChangedHandler;
            this.WebBrowser.ObjectForScripting = this;
        }

        protected override void OnAuthenticate()
        {
            this.zoomed = false;
            this.statusCode = 0;
            this.ShowBrowser();

            base.OnAuthenticate();
        }

        public void ShowBrowser()
        {
            DialogResult uiResult = this.ShowDialog(this.ownerWindow);

            switch (uiResult)
            {
                case DialogResult.OK:
                    break;
                case DialogResult.Cancel:
                    throw new AdalException(AdalError.AuthenticationCanceled);
                default:
                    throw this.CreateExceptionForAuthenticationUiFailed(this.statusCode);
            }
        }

        protected override void WebBrowserNavigatingHandler(object sender, WebBrowserNavigatingEventArgs e)
        {
            this.SetBrowserZoom();
            base.WebBrowserNavigatingHandler(sender, e);
        }

        protected override void OnClosingUrl()
        {
            this.DialogResult = DialogResult.OK;
        }

        protected override void OnNavigationCanceled(int statusCode)
        {
            this.statusCode = statusCode;
            this.DialogResult = (statusCode == 0) ? DialogResult.Cancel : DialogResult.Abort;
        }

        private void SetBrowserZoom()
        {
            int windowsZoomPercent = DpiHelper.ZoomPercent;
            if (NativeWrapper.NativeMethods.IsProcessDPIAware() && 100 != windowsZoomPercent && !this.zoomed)
            {
                // There is a bug in some versions of the IE browser control that causes it to 
                // ignore scaling unless it is changed.
                this.SetBrowserControlZoom(windowsZoomPercent - 1);
                this.SetBrowserControlZoom(windowsZoomPercent);

                this.zoomed = true;
            }
        }

        private void SetBrowserControlZoom(int zoomPercent)
        {
            NativeWrapper.IWebBrowser2 browser2 = (NativeWrapper.IWebBrowser2)this.WebBrowser.ActiveXInstance;
            NativeWrapper.IOleCommandTarget cmdTarget = browser2.Document as NativeWrapper.IOleCommandTarget;
            if (cmdTarget != null)
            {
                const int OLECMDID_OPTICAL_ZOOM = 63;
                const int OLECMDEXECOPT_DONTPROMPTUSER = 2;

                object[] commandInput = { zoomPercent };

                int hResult = cmdTarget.Exec(
                    IntPtr.Zero, OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT_DONTPROMPTUSER, commandInput, IntPtr.Zero);
                Marshal.ThrowExceptionForHR(hResult);
            }
        }

        private void FormShownHandler(object sender, EventArgs e)
        {
            // If we don't have an owner we need to make sure that the pop up browser 
            // window is on top of other windows.  Activating the window will accomplish this.
            if (null == this.Owner)
            {
                this.Activate();
            }
        }

        private void WebBrowserDocumentTitleChangedHandler(object sender, EventArgs e)
        {
            this.Text = this.WebBrowser.DocumentTitle;
        }
    }
}
