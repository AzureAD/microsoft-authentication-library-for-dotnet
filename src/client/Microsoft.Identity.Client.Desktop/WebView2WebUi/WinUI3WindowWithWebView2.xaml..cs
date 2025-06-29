// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// #if WINRT
// using System;
// using System.Threading;
// using System.Threading.Tasks;
// //using System.Windows.Forms;
// using Microsoft.Identity.Client.Core;
// using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
// using Microsoft.Identity.Client.UI;
// using Microsoft.UI.Xaml;
// using Microsoft.UI.Xaml.Controls;
// using Microsoft.UI.Windowing;
// using Microsoft.Web.WebView2.Core;
// using Windows.Graphics;

// namespace Microsoft.Identity.Client.Desktop.WebView2WebUi
// {
//     internal class WinUI3WindowWithWebview2 : Window
//     {
//         private const int UIWidth = 566;
//         private readonly EmbeddedWebViewOptions _embeddedWebViewOptions;
//         private readonly ILoggerAdapter _logger;
//         private readonly Uri _startUri;
//         private readonly Uri _endUri;
//         private WebView2 _webView2;
//         //private const string WebView2UserDataFolder = "%UserProfile%/.msal/webview2/data";

//         private AuthorizationResult _result;
//         private TaskCompletionSource<AuthorizationResult> _dialogCompletionSource;
//         private CancellationToken _cancellationToken;

//         public WinUI3WindowWithWebview2(
//             Window ownerWindow,
//             EmbeddedWebViewOptions embeddedWebViewOptions,
//             ILoggerAdapter logger,
//             Uri startUri,
//             Uri endUri)
//         {
//             _embeddedWebViewOptions = embeddedWebViewOptions ?? EmbeddedWebViewOptions.GetDefaultOptions();
//             _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//             _startUri = startUri ?? throw new ArgumentNullException(nameof(startUri));
//             _endUri = endUri ?? throw new ArgumentNullException(nameof(endUri));

//             InitializeComponent(ownerWindow);
//         }

//         public async Task<AuthorizationResult> DisplayDialogAndInterceptUriAsync(CancellationToken cancellationToken)
//         {
//             _cancellationToken = cancellationToken;
//             _dialogCompletionSource = new TaskCompletionSource<AuthorizationResult>();

//             // Register cancellation callback
//             using (cancellationToken.Register(CloseIfOpen))
//             {
//                 // Set up WebView2 event handlers
//                 _webView2.NavigationStarting += WebView2Control_NavigationStarting;

//                 // Ensure WebView2 is initialized
//                 _webView2.CoreWebView2Initialized += WebView2Control_CoreWebView2Initialized;

//                 // Start navigation
//                 _webView2.Source = _startUri;

//                 // Activate and show the window
//                 this.Activate();

//                 try
//                 {
//                     // Wait for the dialog to complete
//                     return await _dialogCompletionSource.Task.ConfigureAwait(false);
//                 }
//                 catch (TaskCanceledException)
//                 {
//                     return AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
//                 }
//             }
//         }

//         private void WebView2Control_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
//         {
//             if (args.Exception != null)
//             {
//                 // Handle initialization error
//                 _dialogCompletionSource?.TrySetResult(
//                     AuthorizationResult.FromStatus(AuthorizationStatus.ErrorHttp));
//                 return;
//             }

//             // Configure WebView2 after successful initialization
//             ConfigureWebView2();
//         }

//         private void CloseIfOpen()
//         {
//             DispatcherQueue.TryEnqueue(() =>
//             {
//                 if (_dialogCompletionSource != null && !_dialogCompletionSource.Task.IsCompleted)
//                 {
//                     _result = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
//                     _dialogCompletionSource.SetResult(_result);
//                 }
//                 this.Close();
//             });
//         }

//         private void CompleteDialog(AuthorizationResult result)
//         {
//             _result = result;
//             if (_dialogCompletionSource != null && !_dialogCompletionSource.Task.IsCompleted)
//             {
//                 _dialogCompletionSource.SetResult(result);
//             }
//             this.Close();
//         }

//         private void InitializeComponent(Window ownerWindow)
//         {
//             // Set window properties
//             this.Title = string.Empty;
//             this.ExtendsContentIntoTitleBar = false;

//             // Calculate window size based on screen dimensions
//             var displayArea = DisplayArea.Primary;
//             int uiHeight = (int)(Math.Max(displayArea.WorkArea.Height, 160) * 0.7);

//             // Set window size
//             this.AppWindow.Resize(new SizeInt32(UIWidth, uiHeight));

//             // Position window
//             if (ownerWindow != null)
//             {
//                 // Center relative to owner window
//                 var ownerPos = ownerWindow.AppWindow.Position;
//                 var ownerSize = ownerWindow.AppWindow.Size;
//                 var centerX = ownerPos.X + (ownerSize.Width - UIWidth) / 2;
//                 var centerY = ownerPos.Y + (ownerSize.Height - uiHeight) / 2;
//                 this.AppWindow.Move(new PointInt32(centerX, centerY));
//             }
//             else
//             {
//                 // Center on screen
//                 var centerX = (displayArea.WorkArea.Width - UIWidth) / 2;
//                 var centerY = (displayArea.WorkArea.Height - uiHeight) / 2;
//                 this.AppWindow.Move(new PointInt32(centerX, centerY));
//             }

//             // Create WebView2 control
//             _webView2 = new WebView2();

//             //// Set WebView2 creation properties
//             //_webView2.CreationProperties = new CoreWebView2CreationProperties()
//             //{
//             //    UserDataFolder = Environment.ExpandEnvironmentVariables(WebView2UserDataFolder)
//             //};

//             // Create main grid
//             var mainGrid = new Grid();
//             mainGrid.Children.Add(_webView2);

//             // Set window content
//             this.Content = mainGrid;

//             // Window event handlers
//             this.Closed += (s, e) =>
//             {
//                 if (_result == null)
//                 {
//                     _result = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
//                 }

//                 if (_dialogCompletionSource != null && !_dialogCompletionSource.Task.IsCompleted)
//                 {
//                     _dialogCompletionSource.SetResult(_result);
//                 }
//             };

//             this.Activated += (s, e) =>
//             {
//                 // Ensure window stays on top if no owner
//                 if (ownerWindow == null)
//                 {
//                     this.AppWindow.IsShownInSwitchers = true;
//                 }
//             };
//         }
//         private void WebView2Control_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
//         {
//             if (CheckForEndUrl(new Uri(e.Uri)))
//             {
//                 _logger.Verbose(() => "[WebView2Control] Redirect URI reached. Stopping the interactive view");
//                 e.Cancel = true;
//             }
//             else
//             {
//                 _logger.Verbose(() => "[WebView2Control] Navigating to " + e.Uri);
//             }
//         }

//         private bool CheckForEndUrl(Uri url)
//         {
//             bool readyToClose = false;

//             if (url.Authority.Equals(_endUri.Authority, StringComparison.OrdinalIgnoreCase) &&
//                 url.AbsolutePath.Equals(_endUri.AbsolutePath))
//             {
//                 _logger.Info("Redirect Uri was reached. Stopping WebView navigation...");
//                 var result = AuthorizationResult.FromUri(url.OriginalString);

//                 DispatcherQueue.TryEnqueue(() => CompleteDialog(result));
//                 readyToClose = true;
//             }

//             if (!readyToClose && !EmbeddedUiCommon.IsAllowedIeOrEdgeAuthorizationRedirect(url))
//             {
//                 _logger.Error($"[WebView2Control] Redirection to url: {url} is not permitted - WebView2 will fail...");

//                 var result = AuthorizationResult.FromStatus(
//                     AuthorizationStatus.ErrorHttp,
//                     MsalError.NonHttpsRedirectNotSupported,
//                     MsalErrorMessage.NonHttpsRedirectNotSupported);

//                 DispatcherQueue.TryEnqueue(() => CompleteDialog(result));
//                 readyToClose = true;
//             }

//             return readyToClose;
//         }

//         private void ConfigureWebView2()
//         {
//             if (_webView2.CoreWebView2 != null)
//             {
//                 _webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
//                 _webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
//                 _webView2.CoreWebView2.Settings.AreHostObjectsAllowed = false;
//                 _webView2.CoreWebView2.Settings.IsScriptEnabled = true;
//                 _webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
//                 _webView2.CoreWebView2.Settings.IsStatusBarEnabled = true;
//                 _webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

//                 if (_embeddedWebViewOptions.Title == null)
//                 {
//                     _webView2.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
//                 }
//                 else
//                 {
//                     DispatcherQueue.TryEnqueue(() => this.Title = _embeddedWebViewOptions.Title);
//                 }
//             }
//         }

//         private void CoreWebView2_DocumentTitleChanged(object sender, object e)
//         {
//             DispatcherQueue.TryEnqueue(() =>
//             {
//                 this.Title = _webView2.CoreWebView2.DocumentTitle ?? "";
//             });
//         }
//     }
// }

// #endif

#if WINRT
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
using Microsoft.Web.WebView2.Core;
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
        // private WebView2 _webView2;
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
            this.InitializeComponent();
            
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
            
            // Configure window properties
            ConfigureWindow();
            
            // Set up WebView2 creation properties
            // ConfigureWebView2CreationProperties();
            
            // Set up WebView2 event handlers
            _webView2.NavigationStarting += WebView2Control_NavigationStarting;
            _webView2.CoreWebView2Initialized += WebView2Control_CoreWebView2Initialized;

            // Start navigation
            _webView2.Source = _startUri;

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

                // Initialize WebView2 with custom environment if needed
                //var env = await CoreWebView2Environment.CreateAsync(
                //    null,
                //    Environment.ExpandEnvironmentVariables(WebView2UserDataFolder)).ConfigureAwait(false);
                
                //await _webView2.EnsureCoreWebView2Async(env).ConfigureAwait(false);

                // Activate and show the window
                this.Activate();

                try
                {
                    // Wait for the dialog to complete
                    return await _dialogCompletionSource.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
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
            this.Title = string.Empty;
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

            // Window event handlers
            this.Closed += (s, e) =>
            {
                if (_result == null)
                {
                    _result = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                }

                if (_dialogCompletionSource != null && !_dialogCompletionSource.Task.IsCompleted)
                {
                    _dialogCompletionSource.SetResult(_result);
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

        // private void ConfigureWebView2CreationProperties()
        // {
        //     try
        //     {
        //         // Set WebView2 creation properties
        //         _webView2.CreationProperties = new CoreWebView2CreationProperties()
        //         {
        //             UserDataFolder = Environment.ExpandEnvironmentVariables(WebView2UserDataFolder)
        //         };
                
        //         _logger.Info("WebView2 creation properties configured successfully");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.Warning($"Failed to set WebView2 creation properties: {ex.Message}");
        //     }
        // }

        private void WebView2Control_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs e)
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
                // Complete the dialog with the result
                _dialogCompletionSource?.TrySetResult(_result);

                // Close the window
                DispatcherQueue.TryEnqueue(() => this.Close());
            }

            return readyToClose;
        }

        private void WebView2Control_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            // if (args.Exception != null)
            // {
            //     _logger.Error($"[WebView2Control] CoreWebView2 initialization failed: {args.Exception.Message}");
            //     _dialogCompletionSource?.TrySetException(args.Exception);
            //     return;
            // }

            // _logger.Verbose(() => "[WebView2Control] CoreWebView2InitializationCompleted ");

            // // Configure WebView2 settings
            // _webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            // _webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            // _webView2.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            // _webView2.CoreWebView2.Settings.IsScriptEnabled = true;
            // _webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
            // _webView2.CoreWebView2.Settings.IsStatusBarEnabled = true;
            // _webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

            // // Handle title changes
            // if (_embeddedWebViewOptions.Title == null)
            // {
            //     _webView2.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            // }
            // else
            // {
            //     this.Title = _embeddedWebViewOptions.Title;
            // }
            if (args.Exception != null)
            {
                _logger.Error($"[WebView2Control] CoreWebView2 initialization failed: {args.Exception.Message}");
            }
            else
            {
                SetTitle(sender);
            }
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
            // Correctly reference the 'Package' type from the 'Windows.ApplicationModel' namespace.
            var packageDisplayName = "Microsoft Authentication Library";
            var webView2Version = (webView2 != null) ? " - " + GetWebView2Version(webView2) : string.Empty;
            Title = $"{packageDisplayName}{webView2Version}";
        }

        private string GetWebView2Version(WebView2 webView2)
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
        
        // private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        // {
        //     DispatcherQueue.TryEnqueue(() =>
        //     {
        //         this.Title = _webView2.CoreWebView2.DocumentTitle ?? "";
        //     });
        // }

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
