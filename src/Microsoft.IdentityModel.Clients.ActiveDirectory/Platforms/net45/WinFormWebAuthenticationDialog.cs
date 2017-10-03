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

        /// <summary>
        /// 
        /// </summary>
        protected override void OnAuthenticate()
        {
            this.zoomed = false;
            this.statusCode = 0;
            this.ShowBrowser();

            base.OnAuthenticate();
        }


        /// <summary>
        /// 
        /// </summary>
        public void ShowBrowser()
        {
            DialogResult uiResult = this.ShowDialog(this.ownerWindow);

            switch (uiResult)
            {
                case DialogResult.OK:
                    break;
                case DialogResult.Cancel:
                    this.Result = new AuthorizationResult(AuthorizationStatus.UserCancel, null);
                    break;
                default:
                    throw this.CreateExceptionForAuthenticationUiFailed(this.statusCode);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void WebBrowserNavigatingHandler(object sender, WebBrowserNavigatingEventArgs e)
        {
            this.SetBrowserZoom();
            base.WebBrowserNavigatingHandler(sender, e);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnClosingUrl()
        {
            this.DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnNavigationCanceled(int inputStatusCode)
        {
            this.statusCode = inputStatusCode;
            this.DialogResult = (inputStatusCode == 0) ? DialogResult.Cancel : DialogResult.Abort;
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
