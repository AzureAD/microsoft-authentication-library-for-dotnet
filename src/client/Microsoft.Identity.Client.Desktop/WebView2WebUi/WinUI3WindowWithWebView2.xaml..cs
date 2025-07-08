// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if WINRT
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
    internal sealed partial class WinUI3WindowWithWebView2 : Window
    {
        private const int UIWidth = 566;
        private readonly EmbeddedWebViewOptions _embeddedWebViewOptions;
        private readonly ILoggerAdapter _logger;
        private readonly Uri _startUri;
        private readonly Uri _endUri;
        private const string WebView2UserDataFolder = "%UserProfile%/.msal/webview2/data";

        private AuthorizationResult _result;
        private TaskCompletionSource<AuthorizationResult> _dialogCompletionSource;
        private CancellationToken _cancellationToken;
        private Window _ownerWindow;

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
            // XAML InitializeComponent() - this loads the XAML UI
            this.InitializeComponent();
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", "%UserProfile%/.msal/webview2/data");
            _embeddedWebViewOptions = embeddedWebViewOptions ?? EmbeddedWebViewOptions.GetDefaultOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _startUri = startUri ?? throw new ArgumentNullException(nameof(startUri));
            _endUri = endUri ?? throw new ArgumentNullException(nameof(endUri));

            if (ownerWindow is Window window)
            {
                _ownerWindow = window;
            }
            else if (ownerWindow != null)
            {
                // Handle other window types if needed
                _logger.Warning("Owner window type not directly supported in WinUI 3");
            }

            // Set up WebView2 event handlers
            _webView2.CoreWebView2Initialized += WebView2_CoreWebView2Initialized;
            _webView2.NavigationStarting += WebView2_NavigationStarting;

            // Configure window properties - CRITICAL FOR PROPER TASK COMPLETION
            ConfigureWindow();
            
            StatusUpdate("Ready");
            SetTitle();
        }

        private void StatusUpdate(string message)
        {
            StatusBar.Text = message;
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

            // CRITICAL: Configure WebView2 settings for security and proper authentication
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
            StatusUpdate("WebView2 Initialized - Ready for authentication");
        }

        public async Task<AuthorizationResult> DisplayDialogAndInterceptUriAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _dialogCompletionSource = new TaskCompletionSource<AuthorizationResult>();

            // Register cancellation callback
            using (cancellationToken.Register(CloseIfOpen))
            {
                try
                {  
                    
                    // 1. Create a TaskCompletionSource to wait for the WebView2 initialization
                    var tcs = new TaskCompletionSource<bool>();
                    
                    // 2. Queue the WebView2 initialization on the UI thread
                    this.DispatcherQueue.TryEnqueue(() => 
                    {
                        // First activate the window
                        this.Activate();
                        _logger.Info("Activating authentication window...");
                        
                        // Start the async operation on the UI thread
                        var initTask = _webView2.EnsureCoreWebView2Async().AsTask();
                        
                        // Continue with a callback when it completes
                        initTask.ContinueWith(t => 
                        {
                            if (t.IsFaulted)
                            {
                                _logger.Error($"WebView2 initialization failed: {t.Exception?.Message}");
                                tcs.TrySetException(t.Exception ?? new Exception("Unknown WebView2 initialization error"));
                            }
                            else if (t.IsCanceled)
                            {
                                _logger.Warning("WebView2 initialization was canceled");
                                tcs.TrySetCanceled();
                            }
                            else
                            {
                                _logger.Info("WebView2 CoreWebView2 initialized successfully.");
                                
                                // Set the source URI for authentication
                                _webView2.Source = _startUri;
                                _logger.Info($"Starting authentication flow with URI: {_startUri}");
                                StatusUpdate("Starting navigation...");
                                
                                tcs.TrySetResult(true);
                            }
                        }, TaskScheduler.Current);
                    });
                    
                    // 4. Wait for the initialization to complete
                    await tcs.Task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Authentication failed with exception: {ex.Message}");
                    return AuthorizationResult.FromStatus(AuthorizationStatus.ErrorHttp);
                }

                try
                {
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
            this.Title = "Microsoft Authentication Library";
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

            // CRITICAL: Window event handlers - This completes the TaskCompletionSource
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

        private void Go_OnClick(object sender, RoutedEventArgs e)
        {
            _logger.Verbose(() => "[WebView2Control] Go_OnClick ");
        }

        private void AddressBar_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            _logger.Verbose(() => "[WebView2Control] AddressBar_KeyDown ");
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

        private void OnWindowClosed(object sender, WindowEventArgs e)
        {
            // Ensure the task is completed if the window is closed
            if (_dialogCompletionSource != null && !_dialogCompletionSource.Task.IsCompleted)
            {
                _dialogCompletionSource.TrySetResult(AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel));
            }

            // Clean up WebView2
            if (_webView2 != null)
            {
                _webView2.Close();
            }
        }
    }
}

#endif
