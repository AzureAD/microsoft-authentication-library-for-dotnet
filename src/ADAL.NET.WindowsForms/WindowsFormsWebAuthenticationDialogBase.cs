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

        private const int UIHeightGap = 310;

        private Panel webBrowserPanel;
        private CustomWebBrowser webBrowser;

        private Uri desiredCallbackUri;

        protected string result;

        protected IWin32Window ownerWindow;

        private static readonly int UIHeight = Math.Max(Screen.PrimaryScreen.WorkingArea.Height - UIHeightGap, 160);

        /// <summary>
        /// Default constructor
        /// </summary>
        protected WindowsFormsWebAuthenticationDialogBase()
        {
            this.webBrowser = new CustomWebBrowser();

            this.InitializeComponent();
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

        public object OwnerWindow
        {
            get
            {
                return this.ownerWindow;
            }

            set
            {
                IWin32Window window = value as IWin32Window;
                if (window != null)
                {
                    this.ownerWindow = window;
                }
                else if (value is IntPtr)
                {
                    this.ownerWindow = new WindowsFormsWin32Window { Handle = (IntPtr)value };
                }
                else if (null == value)
                {
                    this.ownerWindow = null;
                }
                else
                {
                    throw new AdalException(AdalError.InvalidOwnerWindowType,
                        "Invalid owner window type. Expected types are IWin32Window or IntPtr (for window handle).");
                }
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

            // we cancel further processing, if we reached final URL.
            // Security issue: we prohibit navigation with auth code
            // if redirect URI is URN, then we prohibit navigation, to prevent random browser popup.
            e.Cancel = this.CheckForClosingUrl(e.Url);
        }

        private void WebBrowserNavigatedHandler(object sender, WebBrowserNavigatedEventArgs e)
        {
            this.CheckForClosingUrl(e.Url);
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
                this.result = url.AbsoluteUri;
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
                    System.Diagnostics.Trace.WriteLine(string.Format("WebBrowser state: IsBusy: {0}, ReadyState: {1}, Created: {2}, Disposing: {3}, IsDisposed: {4}, IsOffline: {5}", this.webBrowser.IsBusy, this.webBrowser.ReadyState, this.webBrowser.Created, this.webBrowser.Disposing, this.webBrowser.IsDisposed, this.webBrowser.IsOffline));
                    this.webBrowser.Stop();
                    System.Diagnostics.Trace.WriteLine(string.Format("WebBrowser state (after Stop): IsBusy: {0}, ReadyState: {1}, Created: {2}, Disposing: {3}, IsDisposed: {4}, IsOffline: {5}", this.webBrowser.IsBusy, this.webBrowser.ReadyState, this.webBrowser.Created, this.webBrowser.Disposing, this.webBrowser.IsDisposed, this.webBrowser.IsOffline));
                }
            }
        }

        protected abstract void OnClosingUrl();

        protected abstract void OnNavigationCanceled(int statusCode);

        public string AuthenticateAAD(Uri requestUri, Uri callbackUri)
        {
            this.desiredCallbackUri = callbackUri;
            this.result = null;

            // The WebBrowser event handlers must not throw exceptions.
            // If they do then they may be swallowed by the native
            // browser com control.
            this.webBrowser.Navigating += this.WebBrowserNavigatingHandler;
            this.webBrowser.Navigated += this.WebBrowserNavigatedHandler;
            this.webBrowser.NavigateError += this.WebBrowserNavigateErrorHandler;

            this.webBrowser.Navigate(requestUri);
            this.OnAuthenticate();

            return this.result;
        }

        protected virtual void OnAuthenticate()
        { }

        private void InitializeComponent()
        {
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
            this.webBrowserPanel.Size = new Size(UIWidth, UIHeight);
            this.webBrowserPanel.TabIndex = 2;

            // 
            // BrowserAuthenticationWindow
            // 
            this.AutoScaleDimensions = new SizeF(6, 13);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(UIWidth, UIHeight);
            this.Controls.Add(this.webBrowserPanel);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Name = "BrowserAuthenticationWindow";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = string.Empty;
            this.ShowIcon = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // If we don't have an owner we need to make sure that the pop up browser 
            // window is in the task bar so that it can be selected with the mouse.
            //
            this.ShowInTaskbar = null == this.OwnerWindow;

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
                string.Format("The browser based authentication dialog failed to complete for an unkown reason. StatusCode: {0}", statusCode)) { StatusCode = statusCode };
        }
    }
}