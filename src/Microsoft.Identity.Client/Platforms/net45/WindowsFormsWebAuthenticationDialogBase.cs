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
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    [ComVisible(true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class WindowsFormsWebAuthenticationDialogBase : Form
    {
        internal RequestContext RequestContext { get; set; }

        private const int UIWidth = 566;
        private static readonly NavigateErrorStatus NavigateErrorStatus = new NavigateErrorStatus();
        private readonly CustomWebBrowser _webBrowser;
        private Uri _desiredCallbackUri;
        private Keys _key = Keys.None;

        /// <summary>
        /// </summary>
        protected IWin32Window ownerWindow { get; set; }

        private Panel _webBrowserPanel;

        /// <summary>
        /// </summary>
        protected WindowsFormsWebAuthenticationDialogBase(object ownerWindow)
        {
            // From MSDN (http://msdn.microsoft.com/en-us/library/ie/dn720860(v=vs.85).aspx): 
            // The net session count tracks the number of instances of the web browser control. 
            // When a web browser control is created, the net session count is incremented. When the control 
            // is destroyed, the net session count is decremented. When the net session count reaches zero, 
            // the session cookies for the process are cleared. SetQueryNetSessionCount can be used to prevent 
            // the session cookies from being cleared for applications where web browser controls are being created 
            // and destroyed throughout the lifetime of the application. (Because the application lives longer than 
            // a given instance, session cookies must be retained for a longer periods of time.
            int sessionCount = NativeMethods.SetQueryNetSessionCount(NativeMethods.SessionOp.SESSION_QUERY);
            if (sessionCount == 0)
            {
                NativeMethods.SetQueryNetSessionCount(NativeMethods.SessionOp.SESSION_INCREMENT);
            }

            if (ownerWindow == null)
            {
                this.ownerWindow = null;
            }
            else if (ownerWindow is IWin32Window)
            {
                this.ownerWindow = (IWin32Window)ownerWindow;
            }
            else if (ownerWindow is IntPtr)
            {
                this.ownerWindow = new WindowsFormsWin32Window { Handle = (IntPtr)ownerWindow };
            }
            else
            {
                throw new MsalException(MsalError.InvalidOwnerWindowType,
                    "Invalid owner window type. Expected types are IWin32Window or IntPtr (for window handle).");
            }

            _webBrowser = new CustomWebBrowser();
            _webBrowser.PreviewKeyDown += WebBrowser_PreviewKeyDown;
            InitializeComponent();
        }

        internal AuthorizationResult Result { get; set; }

        /// <summary>
        /// Gets Web Browser control used by the dialog.
        /// </summary>
        public WebBrowser WebBrowser => _webBrowser;

        private void WebBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                _key = Keys.Back;
            }
        }

        /// <summary>
        /// </summary>
        protected virtual void WebBrowserNavigatingHandler(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                e.Cancel = true;
                return;
            }

            if (_webBrowser.IsDisposed)
            {
                // we cancel all flows in disposed object and just do nothing, let object to close.
                // it just for safety.
                e.Cancel = true;
                return;
            }

            if (_key == Keys.Back)
            {
                //navigation is being done via back key. This needs to be disabled.
                _key = Keys.None;
                e.Cancel = true;
            }

            // we cancel further processing, if we reached final URL.
            // Security issue: we prohibit navigation with auth code
            // if redirect URI is URN, then we prohibit navigation, to prevent random browser popup.
            e.Cancel = CheckForClosingUrl(e.Url);

            // check if the url scheme is of type browser-install://
            // this means we need to launch external browser
            if (e.Url.Scheme.Equals("browser", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(e.Url.AbsoluteUri.Replace("browser://", "https://"));
                e.Cancel = true;
            }

            if (!e.Cancel)
            {
                string urlDecode = MsalHelpers.UrlDecode(e.Url.ToString());
                string message = string.Format(CultureInfo.InvariantCulture, "Navigating to '{0}'.", urlDecode);
                RequestContext.Logger.VerbosePii(message);
            }
        }

        private void WebBrowserNavigatedHandler(object sender, WebBrowserNavigatedEventArgs e)
        {
            // Guard condition
            if (CheckForClosingUrl(e.Url)) return;

            string urlDecode = MsalHelpers.UrlDecode(e.Url.ToString());
            string message = string.Format(CultureInfo.InvariantCulture, "Navigated to '{0}'.", urlDecode);
            RequestContext.Logger.VerbosePii(message);
        }

        /// <summary>
        /// </summary>
        protected virtual void WebBrowserNavigateErrorHandler(object sender, WebBrowserNavigateErrorEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                e.Cancel = true;
                return;
            }

            if (_webBrowser.IsDisposed)
            {
                // we cancel all flow in disposed object.
                e.Cancel = true;
                return;
            }

            if (_webBrowser.ActiveXInstance != e.WebBrowserActiveXInstance)
            {
                // this event came from internal frame, ignore this.
                return;
            }

            if (e.StatusCode >= 300 && e.StatusCode < 400)
            {
                // we could get redirect flows here as well.
                return;
            }

            e.Cancel = true;
            StopWebBrowser();
            // in this handler object could be already disposed, so it should be the last method
            OnNavigationCanceled(e.StatusCode);
        }

        private bool CheckForClosingUrl(Uri url)
        {
            bool readyToClose = false;

            if (url.Authority.Equals(_desiredCallbackUri.Authority, StringComparison.OrdinalIgnoreCase) &&
                url.AbsolutePath.Equals(_desiredCallbackUri.AbsolutePath))
            {
                const string msg = "Redirect Uri was reached. Stopping webview navigation...";
                RequestContext.Logger.Info(msg);
                RequestContext.Logger.InfoPii(msg);
                Result = new AuthorizationResult(AuthorizationStatus.Success, url.OriginalString);
                readyToClose = true;
            }

            if (!readyToClose && !url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                !url.AbsoluteUri.Equals("about:blank", StringComparison.OrdinalIgnoreCase) && !url.Scheme.Equals("javascript", StringComparison.OrdinalIgnoreCase))
            {
                var msg = (string.Format(CultureInfo.InvariantCulture,
                    "Redirection to non-HTTPS scheme ({0}) found! Webview will fail...", url.Scheme));
                RequestContext.Logger.Error(msg);
                RequestContext.Logger.ErrorPii(msg);
                Result = new AuthorizationResult(AuthorizationStatus.ErrorHttp)
                {
                    Error = MsalClientException.NonHttpsRedirectNotSupported,
                    ErrorDescription = MsalErrorMessage.NonHttpsRedirectNotSupported
                };
                readyToClose = true;
            }

            if (readyToClose)
            {
                StopWebBrowser();
                // in this handler object could be already disposed, so it should be the last method
                OnClosingUrl();
            }

            return readyToClose;
        }

        private void StopWebBrowser()
        {
            // Guard condition
            if (_webBrowser.IsDisposed || !_webBrowser.IsBusy) return;

            var msg = string.Format(CultureInfo.InvariantCulture,
                "WebBrowser state: IsBusy: {0}, ReadyState: {1}, Created: {2}, Disposing: {3}, IsDisposed: {4}, IsOffline: {5}",
                _webBrowser.IsBusy, _webBrowser.ReadyState, _webBrowser.Created,
                _webBrowser.Disposing, _webBrowser.IsDisposed, _webBrowser.IsOffline);

            var msgAfterStop = string.Format(CultureInfo.InvariantCulture,
                "WebBrowser state (after Stop): IsBusy: {0}, ReadyState: {1}, Created: {2}, Disposing: {3}, IsDisposed: {4}, IsOffline: {5}",
                _webBrowser.IsBusy, _webBrowser.ReadyState, _webBrowser.Created,
                _webBrowser.Disposing, _webBrowser.IsDisposed, _webBrowser.IsOffline);

            RequestContext.Logger.Verbose(msg);
            RequestContext.Logger.VerbosePii(msg);

            _webBrowser.Stop();

            RequestContext.Logger.Verbose(msgAfterStop);
            RequestContext.Logger.VerbosePii(msgAfterStop);
        }

        /// <summary>
        /// </summary>
        protected abstract void OnClosingUrl();

        /// <summary>
        /// </summary>
        protected abstract void OnNavigationCanceled(int statusCode);

        internal AuthorizationResult AuthenticateAAD(Uri requestUri, Uri callbackUri)
        {
            _desiredCallbackUri = callbackUri;
            Result = null;

            // The WebBrowser event handlers must not throw exceptions.
            // If they do then they may be swallowed by the native
            // browser com control.
            _webBrowser.Navigating += WebBrowserNavigatingHandler;
            _webBrowser.Navigated += WebBrowserNavigatedHandler;
            _webBrowser.NavigateError += WebBrowserNavigateErrorHandler;

            _webBrowser.Navigate(requestUri);
            OnAuthenticate();

            return Result;
        }

        /// <summary>
        /// </summary>
        protected virtual void OnAuthenticate()
        {
        }

        private void InitializeComponent()
        {
            Screen screen = (ownerWindow != null)
                ? Screen.FromHandle(ownerWindow.Handle)
                : Screen.PrimaryScreen;

            // Window height is set to 70% of the screen height.
            int uiHeight = (int)(Math.Max(screen.WorkingArea.Height, 160) * 70.0 / DpiHelper.ZoomPercent);
            _webBrowserPanel = new Panel();
            _webBrowserPanel.SuspendLayout();
            SuspendLayout();

            // webBrowser
            _webBrowser.Dock = DockStyle.Fill;
            _webBrowser.Location = new Point(0, 25);
            _webBrowser.MinimumSize = new Size(20, 20);
            _webBrowser.Name = "webBrowser";
            _webBrowser.Size = new Size(UIWidth, 565);
            _webBrowser.TabIndex = 1;
            _webBrowser.IsWebBrowserContextMenuEnabled = false;

            // webBrowserPanel
            _webBrowserPanel.Controls.Add(_webBrowser);
            _webBrowserPanel.Dock = DockStyle.Fill;
            _webBrowserPanel.BorderStyle = BorderStyle.None;
            _webBrowserPanel.Location = new Point(0, 0);
            _webBrowserPanel.Name = "webBrowserPanel";
            _webBrowserPanel.Size = new Size(UIWidth, uiHeight);
            _webBrowserPanel.TabIndex = 2;

            // BrowserAuthenticationWindow
            AutoScaleDimensions = new SizeF(6, 13);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(UIWidth, uiHeight);
            Controls.Add(_webBrowserPanel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "BrowserAuthenticationWindow";

            // Move the window to the center of the parent window only if owner window is set.
            StartPosition = (ownerWindow != null)
                ? FormStartPosition.CenterParent
                : FormStartPosition.CenterScreen;
            Text = string.Empty;
            ShowIcon = false;
            MaximizeBox = false;
            MinimizeBox = false;

            // If we don't have an owner we need to make sure that the pop up browser 
            // window is in the task bar so that it can be selected with the mouse.
            ShowInTaskbar = (null == ownerWindow);

            _webBrowserPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        /// <summary>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopWebBrowser();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// </summary>
        protected MsalClientException CreateExceptionForAuthenticationUiFailed(int statusCode)
        {
            if (NavigateErrorStatus.Messages.ContainsKey(statusCode))
            {
                string format = "The browser based authentication dialog failed to complete. Reason: {0}";
                string message = string.Format(CultureInfo.InvariantCulture, format, NavigateErrorStatus.Messages[statusCode]);
                return new MsalClientException(MsalClientException.AuthenticationUiFailedError, message);
            }

            string formatUnknown = "The browser based authentication dialog failed to complete for an unknown reason. StatusCode: {0}";
            string messageUnknown = string.Format(CultureInfo.InvariantCulture, formatUnknown, statusCode);
            return new MsalClientException(MsalClientException.AuthenticationUiFailedError, messageUnknown);
        }

        private sealed class WindowsFormsWin32Window : IWin32Window
        {
            public IntPtr Handle { get; set; }
        }

        /// <summary>
        /// </summary>
        protected static class DpiHelper
        {
            static DpiHelper()
            {
                const double DefaultDpi = 96.0;

                const int LOGPIXELSX = 88;
                const int LOGPIXELSY = 90;

                double deviceDpiX;
                double deviceDpiY;

                IntPtr dC = NativeWrapper.NativeMethods.GetDC(IntPtr.Zero);
                if (dC != IntPtr.Zero)
                {
                    deviceDpiX = NativeWrapper.NativeMethods.GetDeviceCaps(dC, LOGPIXELSX);
                    deviceDpiY = NativeWrapper.NativeMethods.GetDeviceCaps(dC, LOGPIXELSY);
                    NativeWrapper.NativeMethods.ReleaseDC(IntPtr.Zero, dC);
                }
                else
                {
                    deviceDpiX = DefaultDpi;
                    deviceDpiY = DefaultDpi;
                }

                int zoomPercentX = (int)(100 * (deviceDpiX / DefaultDpi));
                int zoomPercentY = (int)(100 * (deviceDpiY / DefaultDpi));

                ZoomPercent = Math.Min(zoomPercentX, zoomPercentY);
            }

            /// <summary>
            /// </summary>
            public static int ZoomPercent { get; }
        }

        /// <summary>
        /// </summary>
        internal static class NativeMethods
        {
            [DllImport("IEFRAME.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
            internal static extern int SetQueryNetSessionCount(SessionOp sessionOp);

            internal enum SessionOp
            {
                SESSION_QUERY = 0,
                SESSION_INCREMENT,
                SESSION_DECREMENT
            }
        }
    }
}