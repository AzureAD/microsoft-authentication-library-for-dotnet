// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.net45
{
    /// <summary>
    /// The browser dialog used for user authentication
    /// </summary>
    [ComVisible(true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class WindowsFormsWebAuthenticationDialog : WindowsFormsWebAuthenticationDialogBase
    {
        private int _statusCode;
        private bool _zoomed;

        /// <summary>
        /// Default constructor
        /// </summary>
        public WindowsFormsWebAuthenticationDialog(object ownerWindow)
            : base(ownerWindow)
        {
            Shown += FormShownHandler;
            WebBrowser.DocumentTitleChanged += WebBrowserDocumentTitleChangedHandler;
            WebBrowser.ObjectForScripting = this;
        }

        /// <summary>
        /// </summary>
        protected override void OnAuthenticate()
        {
            _zoomed = false;
            _statusCode = 0;
            ShowBrowser();

            base.OnAuthenticate();
        }

        /// <summary>
        /// </summary>
        public void ShowBrowser()
        {
            DialogResult uiResult = DialogResult.None;
            InvokeHandlingOwnerWindow(() => uiResult = ShowDialog(ownerWindow));

            switch (uiResult)
            {
                case DialogResult.OK:
                    break;
                case DialogResult.Cancel:
                    Result = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                    break;
                default:
                    throw CreateExceptionForAuthenticationUiFailed(_statusCode);
            }
        }

        /// <summary>
        /// </summary>
        protected override void WebBrowserNavigatingHandler(object sender, WebBrowserNavigatingEventArgs e)
        {
            SetBrowserZoom();
            base.WebBrowserNavigatingHandler(sender, e);
        }

        /// <summary>
        /// </summary>
        protected override void OnClosingUrl()
        {
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// </summary>
        protected override void OnNavigationCanceled(int inputStatusCode)
        {
            _statusCode = inputStatusCode;
            DialogResult = (inputStatusCode == 0) ? DialogResult.Cancel : DialogResult.Abort;
        }

        private void SetBrowserZoom()
        {
            int windowsZoomPercent = DpiHelper.ZoomPercent;
            if (NativeWrapper.NativeMethods.IsProcessDPIAware() && 100 != windowsZoomPercent && !_zoomed)
            {
                // There is a bug in some versions of the IE browser control that causes it to
                // ignore scaling unless it is changed.
                SetBrowserControlZoom(windowsZoomPercent - 1);
                SetBrowserControlZoom(windowsZoomPercent);

                _zoomed = true;
            }
        }

        private void SetBrowserControlZoom(int zoomPercent)
        {
            NativeWrapper.IWebBrowser2 browser2 = (NativeWrapper.IWebBrowser2)WebBrowser.ActiveXInstance;
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
            if (null == Owner)
            {
                Activate();
            }
        }

        private void WebBrowserDocumentTitleChangedHandler(object sender, EventArgs e)
        {
            Text = WebBrowser.DocumentTitle;
        }
    }
}
