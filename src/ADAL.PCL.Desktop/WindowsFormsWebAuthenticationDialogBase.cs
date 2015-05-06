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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    [ComVisible(true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class WindowsFormsWebAuthenticationDialogBase : Form
    {
        private static readonly NavigateErrorStatus NavigateErrorStatus = new NavigateErrorStatus();

        private const int UIWidth = 566;

        private Panel webBrowserPanel;
        private readonly CustomWebBrowser webBrowser;

        private Uri desiredCallbackUri;

        protected IWin32Window ownerWindow;

        private Keys key = Keys.None;

        internal AuthorizationResult Result { get; set; }

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
                throw new AdalException(AdalError.InvalidOwnerWindowType, 
                    "Invalid owner window type. Expected types are IWin32Window or IntPtr (for window handle).");
            }

            this.webBrowser = new CustomWebBrowser();
            this.webBrowser.PreviewKeyDown += webBrowser_PreviewKeyDown;
            this.InitializeComponent();
        }
        
        private void webBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                key = Keys.Back;
            }
        }

        /// <summary>
        /// Gets Web Browser control used by the dialog.
        /// </summary>
        public WebBrowser WebBrowser
        {
            get
            {
                return this.webBrowser;
            }
        }

        protected virtual void WebBrowserNavigatingHandler(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (this.webBrowser.IsDisposed)
            {
                // we cancel all flows in disposed object and just do nothing, let object to close.
                // it just for safety.
                e.Cancel = true;
                return;
            }

            if (key == Keys.Back)
            {
                //navigation is being done via back key. This needs to be disabled.
                key = Keys.None;
                e.Cancel = true;
            }

            // we cancel further processing, if we reached final URL.
            // Security issue: we prohibit navigation with auth code
            // if redirect URI is URN, then we prohibit navigation, to prevent random browser popup.
            e.Cancel = this.CheckForClosingUrl(e.Url);

            if (!e.Cancel)
            {
                PlatformPlugin.Logger.Verbose(null, string.Format("Navigating to '{0}'.", EncodingHelper.UrlDecode(e.Url.ToString())));
            }
        }

        private void WebBrowserNavigatedHandler(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (!this.CheckForClosingUrl(e.Url))
            {
                PlatformPlugin.Logger.Verbose(null, string.Format("Navigated to '{0}'.", EncodingHelper.UrlDecode(e.Url.ToString())));
            }
        }

        protected virtual void WebBrowserNavigateErrorHandler(object sender, WebBrowserNavigateErrorEventArgs e)
        {
            // e.StatusCode - Contains error code which we are able to translate this error to text
            // ADAL.Native contains a code for translation.

            if (this.webBrowser.IsDisposed)
            {
                // we cancel all flow in disposed object.
                e.Cancel = true;
                return;
            }

            if (this.webBrowser.ActiveXInstance != e.WebBrowserActiveXInstance)
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
            this.StopWebBrowser();
            // in this handler object could be already disposed, so it should be the last method
            this.OnNavigationCanceled(e.StatusCode);
        }

        private bool CheckForClosingUrl(Uri url)
        {
            if (url.Authority.Equals(this.desiredCallbackUri.Authority, StringComparison.OrdinalIgnoreCase) && url.AbsolutePath.Equals(this.desiredCallbackUri.AbsolutePath))
            {
                this.Result = new AuthorizationResult(AuthorizationStatus.Success, url.OriginalString);
                this.StopWebBrowser();

                // in this handler object could be already disposed, so it should be the last method
                this.OnClosingUrl();
                return true;
            }

            return false;
        }

        private void StopWebBrowser()
        {
            if (!this.webBrowser.IsDisposed)
            {
                if (this.webBrowser.IsBusy)
                {
                    PlatformPlugin.Logger.Verbose(null, string.Format("WebBrowser state: IsBusy: {0}, ReadyState: {1}, Created: {2}, Disposing: {3}, IsDisposed: {4}, IsOffline: {5}", this.webBrowser.IsBusy, this.webBrowser.ReadyState, this.webBrowser.Created, this.webBrowser.Disposing, this.webBrowser.IsDisposed, this.webBrowser.IsOffline));
                    this.webBrowser.Stop();
                    PlatformPlugin.Logger.Verbose(null, string.Format("WebBrowser state (after Stop): IsBusy: {0}, ReadyState: {1}, Created: {2}, Disposing: {3}, IsDisposed: {4}, IsOffline: {5}", this.webBrowser.IsBusy, this.webBrowser.ReadyState, this.webBrowser.Created, this.webBrowser.Disposing, this.webBrowser.IsDisposed, this.webBrowser.IsOffline));
                }
            }
        }

        protected abstract void OnClosingUrl();

        protected abstract void OnNavigationCanceled(int statusCode);

        internal AuthorizationResult AuthenticateAAD(Uri requestUri, Uri callbackUri)
        {
            this.desiredCallbackUri = callbackUri;
            this.Result = null;

            // The WebBrowser event handlers must not throw exceptions.
            // If they do then they may be swallowed by the native
            // browser com control.
            this.webBrowser.Navigating += this.WebBrowserNavigatingHandler;
            this.webBrowser.Navigated += this.WebBrowserNavigatedHandler;
            this.webBrowser.NavigateError += this.WebBrowserNavigateErrorHandler;

            this.webBrowser.Navigate(requestUri);
            this.OnAuthenticate();

            return this.Result;
        }

        protected virtual void OnAuthenticate()
        { }

        private void InitializeComponent()
        {
            Screen screen = (this.ownerWindow != null) ? Screen.FromHandle(this.ownerWindow.Handle) : Screen.PrimaryScreen;

            // Window height is set to 70% of the screen height.
            int uiHeight = (int)(Math.Max(screen.WorkingArea.Height, 160) * 70.0 / DpiHelper.ZoomPercent);
            this.webBrowserPanel = new Panel();
            this.webBrowserPanel.SuspendLayout();
            this.SuspendLayout();

            // 
            // webBrowser
            // 
            this.webBrowser.Dock = DockStyle.Fill;
            this.webBrowser.Location = new Point(0, 25);
            this.webBrowser.MinimumSize = new Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new Size(UIWidth, 565);
            this.webBrowser.TabIndex = 1;
            this.webBrowser.IsWebBrowserContextMenuEnabled = false;

            // 
            // webBrowserPanel
            // 
            this.webBrowserPanel.Controls.Add(this.webBrowser);
            this.webBrowserPanel.Dock = DockStyle.Fill;
            this.webBrowserPanel.BorderStyle = BorderStyle.None;
            this.webBrowserPanel.Location = new Point(0, 0);
            this.webBrowserPanel.Name = "webBrowserPanel";
            this.webBrowserPanel.Size = new Size(UIWidth, uiHeight);
            this.webBrowserPanel.TabIndex = 2;

            // 
            // BrowserAuthenticationWindow
            // 
            this.AutoScaleDimensions = new SizeF(6, 13);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(UIWidth, uiHeight);
            this.Controls.Add(this.webBrowserPanel);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Name = "BrowserAuthenticationWindow";

            // Move the window to the center of the parent window only if owner window is set.
            this.StartPosition = (this.ownerWindow != null) ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
            this.Text = string.Empty;
            this.ShowIcon = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // If we don't have an owner we need to make sure that the pop up browser 
            // window is in the task bar so that it can be selected with the mouse.
            this.ShowInTaskbar = (null == this.ownerWindow);

            this.webBrowserPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        
        private sealed class WindowsFormsWin32Window : IWin32Window
        {
            public IntPtr Handle { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopWebBrowser();
            }

            base.Dispose(disposing);
        }

        protected AdalException CreateExceptionForAuthenticationUiFailed(int statusCode)
        {
            if (NavigateErrorStatus.Messages.ContainsKey(statusCode))
            {
                return new AdalServiceException(
                    AdalError.AuthenticationUiFailed,
                    string.Format("The browser based authentication dialog failed to complete. Reason: {0}", NavigateErrorStatus.Messages[statusCode])) { StatusCode = statusCode };
            }

            return new AdalServiceException(
                AdalError.AuthenticationUiFailed,
                string.Format("The browser based authentication dialog failed to complete for an unknown reason. StatusCode: {0}", statusCode)) { StatusCode = statusCode };
        }

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

            public static int ZoomPercent { get; private set; }
        }


        internal static class NativeMethods
        {
            internal enum SessionOp 
            {
                SESSION_QUERY = 0,
                SESSION_INCREMENT,
                SESSION_DECREMENT
            };

            [DllImport("IEFRAME.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
            internal static extern int SetQueryNetSessionCount(SessionOp sessionOp);        
        }
    }
}