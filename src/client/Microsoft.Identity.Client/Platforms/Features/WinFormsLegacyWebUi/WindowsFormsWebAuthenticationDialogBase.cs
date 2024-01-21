// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    // This class (and related/derived classes) must be public so that COM can see them via ComVisible to attach to the browser.
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
            else if (ownerWindow is IWin32Window window)
            {
                this.ownerWindow = window;
            }
            else if (ownerWindow is IntPtr ptr)
            {
                this.ownerWindow = new Win32Window(ptr);
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
        protected virtual void WebBrowserBeforeNavigateHandler(object sender, WebBrowserBeforeNavigateEventArgs e)
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

            if (string.IsNullOrEmpty(e.Url))
            {
                RequestContext.Logger.Verbose(()=>"[Legacy WebView] URL in BeforeNavigate is null or empty.");
                e.Cancel = true;
                return;
            }

            Uri url = new Uri(e.Url);

            // we cancel further processing, if we reached final URL.
            // Security issue: we prohibit navigation with auth code
            // if redirect URI is URN, then we prohibit navigation, to prevent random browser pop-up.
            e.Cancel = CheckForClosingUrl(url, e.PostData);

            // check if the URL scheme is of type browser-install://
            // this means we need to launch external browser
            if (url.Scheme.Equals("browser", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(url.AbsoluteUri.Replace("browser://", "https://"));
                e.Cancel = true;
            }

            if (!e.Cancel)
            {
                string urlDecode = CoreHelpers.UrlDecode(e.Url);
                RequestContext.Logger.VerbosePii(
                    () => string.Format(CultureInfo.InvariantCulture, "[Legacy WebView] Navigating to '{0}'.", urlDecode),
                    () => string.Empty);
            }
        }

        private void WebBrowserNavigatedHandler(object sender, WebBrowserNavigatedEventArgs e)
        {
            // Guard condition
            if (CheckForClosingUrl(e.Url))
            {
                return;
            }

            string urlDecode = CoreHelpers.UrlDecode(e.Url.ToString());
            RequestContext.Logger.VerbosePii(
                () => string.Format(CultureInfo.InvariantCulture, "[Legacy WebView] Navigated to '{0}'.", urlDecode),
                () => string.Empty);
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

        private bool CheckForClosingUrl(Uri url, byte[] postData = null)
        {
            bool readyToClose = false;

            if (url.Authority.Equals(_desiredCallbackUri.Authority, StringComparison.OrdinalIgnoreCase) &&
                url.AbsolutePath.Equals(_desiredCallbackUri.AbsolutePath))
            {
                RequestContext.Logger.Info("[Legacy WebView] Redirect URI was reached. Stopping WebView navigation...");
                Result = AuthorizationResult.FromPostData(postData);
                readyToClose = true;
            }

            if (!readyToClose && !EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(url)) // IE error pages                
            {
                RequestContext.Logger.Error(string.Format(CultureInfo.InvariantCulture,
                    "[Legacy WebView] Redirection to non-HTTPS uri: {0} - WebView1 will fail...", url));
                Result = AuthorizationResult.FromStatus(
                    AuthorizationStatus.ErrorHttp,
                    MsalError.NonHttpsRedirectNotSupported,
                    MsalErrorMessage.NonHttpsRedirectNotSupported);
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
            InvokeHandlingOwnerWindow(() =>
            {
                // Guard condition
                if (_webBrowser.IsDisposed || !_webBrowser.IsBusy)
                {
                    return;
                }

                RequestContext.Logger.Verbose(()=>string.Format(CultureInfo.InvariantCulture,
                    "[Legacy WebView] WebBrowser state: IsBusy: {0}, ReadyState: {1}, Created: {2}, Disposing: {3}, IsDisposed: {4}, IsOffline: {5}",
                    _webBrowser.IsBusy, _webBrowser.ReadyState, _webBrowser.Created,
                    _webBrowser.Disposing, _webBrowser.IsDisposed, _webBrowser.IsOffline));

                _webBrowser.Stop();

                RequestContext.Logger.Verbose(()=>string.Format(CultureInfo.InvariantCulture,
                    "[Legacy WebView] WebBrowser state (after Stop): IsBusy: {0}, ReadyState: {1}, Created: {2}, Disposing: {3}, IsDisposed: {4}, IsOffline: {5}",
                    _webBrowser.IsBusy, _webBrowser.ReadyState, _webBrowser.Created,
                    _webBrowser.Disposing, _webBrowser.IsDisposed, _webBrowser.IsOffline));
            });
        }

        /// <summary>
        /// </summary>
        protected abstract void OnClosingUrl();

        /// <summary>
        /// </summary>
        protected abstract void OnNavigationCanceled(int statusCode);

        internal AuthorizationResult AuthenticateAAD(Uri requestUri, Uri callbackUri, CancellationToken cancellationToken)
        {
            _desiredCallbackUri = callbackUri;
            Result = null;

            // The WebBrowser event handlers must not throw exceptions.
            // If they do then they may be swallowed by the native
            // browser com control.
            _webBrowser.BeforeNavigate += WebBrowserBeforeNavigateHandler;
            _webBrowser.Navigated += WebBrowserNavigatedHandler;
            _webBrowser.NavigateError += WebBrowserNavigateErrorHandler;

            _webBrowser.Navigate(requestUri);
            OnAuthenticate(cancellationToken);

            return Result;
        }

        /// <summary>
        /// </summary>
        protected virtual void OnAuthenticate(CancellationToken cancellationToken)
        {
        }

        /// <summary>
        /// Some calls need to be made on the UI thread and this is the central place to check if we have an owner
        /// window and if so, ensure we invoke on that proper thread.
        /// </summary>
        /// <param name="action"></param>
        protected void InvokeHandlingOwnerWindow(Action action)
        {
            // We only support Windows Forms (since our dialog is Windows Forms based)
            if (ownerWindow != null && ownerWindow is Control winFormsControl)
            {
                winFormsControl.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Some calls need to be made on the UI thread and this is the central place to do so and if so, ensure we invoke on that proper thread.
        /// </summary>
        /// <param name="action"></param>
        protected void InvokeOnly(Action action)
        {
            if (InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }

        private void InitializeComponent()
        {
            InvokeHandlingOwnerWindow(() =>
            {
                Screen screen = (ownerWindow != null)
                    ? Screen.FromHandle(ownerWindow.Handle)
                    : Screen.PrimaryScreen;

                // Window height is set to 70% of the screen height.
                int uiHeight = (int)(Math.Max(screen.WorkingArea.Height, 160) * 70.0 / WindowsDpiHelper.ZoomPercent);
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
                ShowInTaskbar = null == ownerWindow;

                _webBrowserPanel.ResumeLayout(false);
                ResumeLayout(false);
            });
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
            if (NavigateErrorStatus.Messages.TryGetValue(statusCode, out string statusCodeMessages))
            {
                string format = "The browser based authentication dialog failed to complete. Reason: {0}";
                string message = string.Format(CultureInfo.InvariantCulture, format, statusCodeMessages);
                return new MsalClientException(MsalError.AuthenticationUiFailedError, message);
            }

            string formatUnknown = "The browser based authentication dialog failed to complete for an unknown reason. StatusCode: {0}";
            string messageUnknown = string.Format(CultureInfo.InvariantCulture, formatUnknown, statusCode);
            return new MsalClientException(MsalError.AuthenticationUiFailedError, messageUnknown);
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
