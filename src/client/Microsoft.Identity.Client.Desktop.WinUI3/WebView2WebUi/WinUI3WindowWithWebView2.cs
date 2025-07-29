// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Web.WebView2.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.UI;
using WinRT.Interop;
using Windows.Graphics;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;

namespace Microsoft.Identity.Client.Desktop.WebView2WebUi
{
    internal sealed class WinUI3WindowWithWebView2 : Window
    {
        private const int UIWidth = 566;
        private readonly EmbeddedWebViewOptions _embeddedWebViewOptions;
        private readonly ILoggerAdapter _logger;
        private readonly Uri _startUri;
        private readonly Uri _endUri;
        private AuthorizationResult _result;
        private TaskCompletionSource<AuthorizationResult> _dialogCompletionSource;
        private CancellationToken _cancellationToken;
        private Window _ownerWindow;
        
        // UI controls created programmatically
        private WebView2 _webView2;
        private ProgressRing _progressRing;

        /// <summary>
        /// Initializes a new instance of the WinUI3WindowWithWebView2 class.
        /// </summary>
        /// <param name="ownerWindow">The parent window that owns this authentication dialog.</param>
        /// <param name="embeddedWebViewOptions">Configuration options for the embedded WebView.</param>
        /// <param name="logger">Logger instance for logging authentication flow events.</param>
        /// <param name="startUri">The initial URI to navigate to for authentication.</param>
        /// <param name="endUri">The redirect URI that signals completion of authentication.</param>
        public WinUI3WindowWithWebView2(
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
            else if (ownerWindow is Window window)
            {
                _ownerWindow = window;
            }
            else
            {
                throw new MsalException(MsalError.InvalidOwnerWindowType,
                    "Invalid owner window type. Expected types are IWin32Window or IntPtr (for window handle).");
            }

            // Initialize the UI programmatically instead of using XAML
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // Create the main grid with just one row for the WebView2
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // WebView2 - takes up the entire window
            _webView2 = new WebView2();
            Grid.SetRow(_webView2, 0);
            mainGrid.Children.Add(_webView2);

            // Progress ring (initially hidden) - centered over the WebView2
            _progressRing = new ProgressRing
            {
                IsActive = true,
                Visibility = Visibility.Collapsed,
                Width = 50,
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(_progressRing, 0);
            mainGrid.Children.Add(_progressRing);

            // Set window content
            Content = mainGrid;

            // Set up WebView2 event handlers
            _webView2.CoreWebView2Initialized += WebView2_CoreWebView2Initialized;
            _webView2.NavigationStarting += WebView2_NavigationStarting;

            ConfigureWindow();
            SetTitle();
        }

        private void WebView2_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _logger.Info($"[WebView2Control] NavigationStarting fired for URL: {e.Uri}");

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
                // Complete the dialog with the result
                _dialogCompletionSource?.TrySetResult(_result);

                // Close the window
                DispatcherQueue.TryEnqueue(() => this.Close());
            }

            return readyToClose;
        }

        private void WebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (args.Exception != null)
            {
                _logger.Error($"[WebView2Control] CoreWebView2 initialization failed: {args.Exception.Message}");
                _dialogCompletionSource?.TrySetResult(
                    AuthorizationResult.FromStatus(AuthorizationStatus.ErrorHttp));
                return;
            }

            _logger.Info("[WebView2Control] CoreWebView2InitializationCompleted");

            _webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView2.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            _webView2.CoreWebView2.Settings.IsScriptEnabled = true;
            _webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
            _webView2.CoreWebView2.Settings.IsStatusBarEnabled = true;
            _webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

            // Handle title changes
            if (_embeddedWebViewOptions.Title == null)
            {
                _webView2.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            }
            else
            {
                this.Title = _embeddedWebViewOptions.Title;
            }

            SetTitle(sender);
        }

        /// <summary>
        /// Displays the authentication dialog and waits for the user to complete the authentication flow.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the authentication operation.</param>
        /// <returns>A task that represents the asynchronous authentication operation. The task result contains the authorization result.</returns>
        public async Task<AuthorizationResult> DisplayDialogAndInterceptUriAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _dialogCompletionSource = new TaskCompletionSource<AuthorizationResult>();

            // Register cancellation callback
            using (cancellationToken.Register(CloseIfOpen))
            {
                try
                {
                    _logger.Info("Starting DisplayDialogAndInterceptUriAsync...");

                    // Activate the window first
                    InvokeHandlingOwnerWindow(() =>
                    {
                        _logger.Info("Activating authentication window...");
                        this.Activate();
                    });

                    // Ensure WebView2 is properly initialized by creating a Core instance if needed
                    var initTcs = new TaskCompletionSource<bool>();

#pragma warning disable VSTHRD101 // Avoid using async lambda for a void returning delegate type
                    InvokeHandlingOwnerWindow(async () =>
                    {
                        try
                        {
                            // Create explicit environment with proper user data folder
                            var userDataFolder = Environment.ExpandEnvironmentVariables("%UserProfile%/.msal/webview2/data");
                            _logger.Info($"Initializing WebView2 with user data folder: {userDataFolder}");

                            // Create directory if it doesn't exist
                            System.IO.Directory.CreateDirectory(userDataFolder);

                            // Create the environment - use the correct parameter order
                            var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions());

                            // If the CoreWebView2 is not initialized yet, initialize it
                            if (_webView2.CoreWebView2 == null)
                            {
                                _logger.Info("WebView2 CoreWebView2 not initialized, initializing now...");

                                // This initializes the CoreWebView2
                                await _webView2.EnsureCoreWebView2Async(env);
                                _logger.Info("WebView2 CoreWebView2 initialized successfully.");
                            }

                            // Now navigate to the start URI
                            _logger.Info($"Starting navigation to: {_startUri}");
                            _webView2.Source = _startUri;

                            initTcs.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Failed to initialize WebView2 or start navigation: {ex.Message}");
                            initTcs.TrySetException(ex);
                        }
                    });
#pragma warning restore VSTHRD101

                    // Wait for initialization and navigation to start
                    await initTcs.Task.ConfigureAwait(false);

                    // Wait for the authentication to complete
                    _logger.Info("Waiting for authentication to complete...");
                    var result = await _dialogCompletionSource.Task.ConfigureAwait(false);
                    _logger.Info($"Authentication completed with status: {result.Status}");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Authentication failed with exception: {ex.Message}");
                    return AuthorizationResult.FromStatus(AuthorizationStatus.ErrorHttp);
                }
            }
        }

        /// <summary>
        /// Some calls need to be made on the UI thread and this is the central place to check if we have an owner
        /// window and if so, ensure we invoke on that proper thread.
        /// </summary>
        /// <param name="action">The action to execute on the UI thread</param>
        private void InvokeHandlingOwnerWindow(Action action)
        {
            // If we have an owner window, use its dispatcher queue
            if (_ownerWindow != null && _ownerWindow.DispatcherQueue != null)
            {
                // If we're already on the UI thread of the owner window, execute directly
                if (_ownerWindow.DispatcherQueue.HasThreadAccess)
                {
                    action();
                }
                else
                {
                    // Otherwise, queue the action to run on the UI thread
                    _ownerWindow.DispatcherQueue.TryEnqueue(() => action());
                }
            }
            else
            {
                // Use our own dispatcher queue if owner window isn't available
                if (DispatcherQueue.HasThreadAccess)
                {
                    action();
                }
                else
                {
                    DispatcherQueue.TryEnqueue(() => action());
                }
            }
        }

        private void CloseIfOpen()
        {
            // Close the window if cancellation is requested
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_dialogCompletionSource != null && !_dialogCompletionSource.Task.IsCompleted)
                {
                    _dialogCompletionSource.TrySetResult(AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel));
                }
                this.Close();
            });
        }

        private void ConfigureWindow()
        {
            // Set window properties
            // this.Title = "Microsoft Authentication Library";
            this.ExtendsContentIntoTitleBar = false;

            // Calculate window size based on screen dimensions
            var displayArea = DisplayArea.Primary;
            int uiHeight = (int)(Math.Max(displayArea.WorkArea.Height, 160) * 0.7);

            // Set window size
            this.AppWindow.Resize(new SizeInt32(UIWidth, uiHeight));

            // Position window
            if (_ownerWindow != null)
            {
                // Center relative to owner window
                var ownerPos = _ownerWindow.AppWindow.Position;
                var ownerSize = _ownerWindow.AppWindow.Size;
                var centerX = ownerPos.X + (ownerSize.Width - UIWidth) / 2;
                var centerY = ownerPos.Y + (ownerSize.Height - uiHeight) / 2;
                this.AppWindow.Move(new PointInt32(centerX, centerY));
            }
            else
            {
                // Center on screen
                var centerX = (displayArea.WorkArea.Width - UIWidth) / 2;
                var centerY = (displayArea.WorkArea.Height - uiHeight) / 2;
                this.AppWindow.Move(new PointInt32(centerX, centerY));
            }

            this.Closed += (s, e) =>
            {
                if (_result == null)
                {
                    _result = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                }

                if (_dialogCompletionSource != null && !_dialogCompletionSource.Task.IsCompleted)
                {
                    _dialogCompletionSource.TrySetResult(_result);
                }
            };

            this.Activated += (s, e) =>
            {
                // Ensure window stays on top if no owner
                if (_ownerWindow == null)
                {
                    this.AppWindow.IsShownInSwitchers = true;
                }
            };
        }

        private void SetTitle(WebView2 webView2 = null)
        {
            var packageDisplayName = "Microsoft Authentication Library";
            var webView2Version = (webView2 != null) ? " - " + GetWebView2Version(webView2) : string.Empty;
            Title = $"{packageDisplayName}{webView2Version}";
        }

        private string GetWebView2Version(WebView2 webView2)
        {
            try
            {
                var runtimeVersion = webView2.CoreWebView2.Environment.BrowserVersionString;

                CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions();
                var targetVersionMajorAndRest = options.TargetCompatibleBrowserVersion;
                var versionList = targetVersionMajorAndRest.Split('.');
                if (versionList.Length != 4)
                {
                    return "Invalid SDK build version";
                }
                var sdkVersion = versionList[2] + "." + versionList[3];

                return $"{runtimeVersion}; {sdkVersion}";
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to get WebView2 version: {ex.Message}");
                return "Unknown";
            }
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.Title = _webView2.CoreWebView2.DocumentTitle ?? "";
            });
        }
    }
}
