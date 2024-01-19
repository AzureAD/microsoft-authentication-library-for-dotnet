// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.UI;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Microsoft.Identity.Client.Platforms.Features.WebView2WebUi
{

    internal class WinFormsPanelWithWebView2 : Form
    {
        private const int UIWidth = 566;
        private readonly EmbeddedWebViewOptions _embeddedWebViewOptions;
        private readonly ILoggerAdapter _logger;
        private readonly Uri _startUri;
        private readonly Uri _endUri;
        private WebView2 _webView2;
        private const string WebView2UserDataFolder = "%UserProfile%/.msal/webview2/data";

        private AuthorizationResult _result;

        private IWin32Window _ownerWindow;

        public WinFormsPanelWithWebView2(
         object ownerWindow,
         EmbeddedWebViewOptions embeddedWebViewOptions,
         ILoggerAdapter logger,
         Uri startUri,
         Uri endUri)
        {
            _embeddedWebViewOptions = embeddedWebViewOptions ?? EmbeddedWebViewOptions.GetDefaultOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _startUri = startUri ?? throw new ArgumentNullException(nameof(startUri));
            _endUri = endUri ?? throw new ArgumentNullException(nameof(endUri));

            if (ownerWindow == null)
            {
                _ownerWindow = null;
            }
            else if (ownerWindow is IWin32Window window)
            {
                _ownerWindow = window;
            }
            else if (ownerWindow is IntPtr ptr && ptr != IntPtr.Zero)
            {
                _ownerWindow = new Win32Window(ptr);
            }
            else
            {
                throw new MsalException(MsalError.InvalidOwnerWindowType,
                    "Invalid owner window type. Expected types are IWin32Window or IntPtr (for window handle).");
            }

            InitializeComponent();

            _webView2.CreationProperties = new CoreWebView2CreationProperties()
            {
                UserDataFolder = Environment.ExpandEnvironmentVariables(WebView2UserDataFolder)
            };
        }

        public AuthorizationResult DisplayDialogAndInterceptUri(CancellationToken cancellationToken)
        {
            _webView2.CoreWebView2InitializationCompleted += WebView2Control_CoreWebView2InitializationCompleted;
            _webView2.NavigationStarting += WebView2Control_NavigationStarting;

            // Starts the navigation
            _webView2.Source = _startUri;
            DisplayDialog(cancellationToken);

            return _result;
        }

        private void DisplayDialog(CancellationToken cancellationToken)
        {
            DialogResult uiResult = DialogResult.None;

            using (cancellationToken.Register(CloseIfOpen))
            {
                InvokeHandlingOwnerWindow(() => uiResult = ShowDialog(_ownerWindow));
                cancellationToken.ThrowIfCancellationRequested();
            }
             
            switch (uiResult)
            {
                case DialogResult.OK:
                    break;
                case DialogResult.Cancel:
                    _result = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                    break;
                default:
                    throw new MsalClientException(
                        "webview2_unexpectedResult",
                        "WebView2 returned an unexpected result: " + uiResult);
            }
        }

        private void CloseIfOpen()
        {
            if (Application.OpenForms.OfType<WinFormsPanelWithWebView2>().Any())
            {
                InvokeOnly(Close);
            }
        }

        private void PlaceOnTop(object sender, EventArgs e)
        {
            // If we don't have an owner we need to make sure that the pop up browser
            // window is on top of other windows.  Activating the window will accomplish this.
            if (null == Owner)
            {
                Activate();
            }
        }

        /// <summary>
        /// Some calls need to be made on the UI thread and this is the central place to check if we have an owner
        /// window and if so, ensure we invoke on that proper thread.
        /// </summary>
        /// <param name="action"></param>
        private void InvokeHandlingOwnerWindow(Action action)
        {
            // We only support WindowsForms (since our dialog is Win Forms based)
            if (_ownerWindow != null && _ownerWindow is Control winFormsControl)
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
        private void InvokeOnly(Action action)
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
                Screen screen = (_ownerWindow != null)
                    ? Screen.FromHandle(_ownerWindow.Handle)
                    : Screen.PrimaryScreen;

                // Window height is set to 70% of the screen height.
                int uiHeight = (int)(Math.Max(screen.WorkingArea.Height, 160) * 70.0 / WindowsDpiHelper.ZoomPercent);
                var webBrowserPanel = new Panel();
                webBrowserPanel.SuspendLayout();
                SuspendLayout();

                // webBrowser
                _webView2 = new WebView2();
                _webView2.Dock = DockStyle.Fill;
                _webView2.Location = new Point(0, 25);
                _webView2.MinimumSize = new Size(20, 20);
                _webView2.Name = "WebView2";
                _webView2.Size = new Size(UIWidth, 565);
                _webView2.TabIndex = 1;

                // webBrowserPanel
                webBrowserPanel.Controls.Add(_webView2);
                webBrowserPanel.Dock = DockStyle.Fill;
                webBrowserPanel.BorderStyle = BorderStyle.None;
                webBrowserPanel.Location = new Point(0, 0);
                webBrowserPanel.Name = "webBrowserPanel";
                webBrowserPanel.Size = new Size(UIWidth, uiHeight);
                webBrowserPanel.TabIndex = 2;

                // BrowserAuthenticationWindow
                AutoScaleDimensions = new SizeF(6, 13);
                AutoScaleMode = AutoScaleMode.Font;
                ClientSize = new Size(UIWidth, uiHeight);
                Controls.Add(webBrowserPanel);
                FormBorderStyle = FormBorderStyle.FixedSingle;
                Name = "BrowserAuthenticationWindow";

                // Move the window to the center of the parent window only if owner window is set.
                StartPosition = (_ownerWindow != null)
                    ? FormStartPosition.CenterParent
                    : FormStartPosition.CenterScreen;
                Text = string.Empty;
                ShowIcon = false;
                MaximizeBox = false;
                MinimizeBox = false;

                // If we don't have an owner we need to make sure that the pop up browser
                // window is in the task bar so that it can be selected with the mouse.
                ShowInTaskbar = null == _ownerWindow;

                webBrowserPanel.ResumeLayout(false);
                ResumeLayout(false);
            });

            this.Shown += PlaceOnTop;
        }

        private void WebView2Control_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (CheckForEndUrl(new Uri(e.Uri)))
            {
                _logger.Verbose(() => "[WebView2Control] Redirect URI reached. Stopping the interactive view");
                e.Cancel = true;
            }
            else
            {
                _logger.Verbose(() => "[WebView2Control] Navigating to " + e.Uri);
            }
        }

        private bool CheckForEndUrl(Uri url)
        {
            bool readyToClose = false;

            if (url.Authority.Equals(_endUri.Authority, StringComparison.OrdinalIgnoreCase) &&
                url.AbsolutePath.Equals(_endUri.AbsolutePath))
            {
                _logger.Info("Redirect Uri was reached. Stopping WebView navigation...");
                _result = AuthorizationResult.FromUri(url.OriginalString);
                readyToClose = true;
            }

            if (!readyToClose && !EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(url)) 
            {
                _logger.Error($"[WebView2Control] Redirection to url: {url} is not permitted - WebView2 will fail...");

                _result = AuthorizationResult.FromStatus(
                    AuthorizationStatus.ErrorHttp,
                    MsalError.NonHttpsRedirectNotSupported,
                    MsalErrorMessage.NonHttpsRedirectNotSupported);

                readyToClose = true;
            }

            if (readyToClose)
            {
                // This should close the dialog
                DialogResult = DialogResult.OK;
            }

            return readyToClose;
        }

        private void WebView2Control_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            _logger.Verbose(() => "[WebView2Control] CoreWebView2InitializationCompleted ");
            _webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView2.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            _webView2.CoreWebView2.Settings.IsScriptEnabled = true;
            _webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
            _webView2.CoreWebView2.Settings.IsStatusBarEnabled = true;
            _webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

            if (_embeddedWebViewOptions.Title == null)
            {
                _webView2.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            }
            else
            {
                Text = _embeddedWebViewOptions.Title;
            }
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            Text = _webView2.CoreWebView2.DocumentTitle ?? "";
        }
    }
}
