// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.Identity.Client.Desktop
{
    internal class MsalDesktopWebUiFactory : IWebUIFactory
    {
        private readonly bool _fallbackToLegacyWebBrowser;
        private readonly Func<string, bool> _isWebView2AvailableFunc;

        public MsalDesktopWebUiFactory(
            bool fallbackToLegacyWebBrowser = false,
            Func<string, bool> isWebView2AvailableForTest = null)
        {
            _fallbackToLegacyWebBrowser = fallbackToLegacyWebBrowser;
            _isWebView2AvailableFunc = isWebView2AvailableForTest ?? IsWebView2Available;
        }

        public bool IsSystemWebViewAvailable => true;

        public IWebUI CreateAuthenticationDialog(
            CoreUIParent coreUIParent, 
            WebViewPreference useEmbeddedWebView, 
            RequestContext requestContext)
        {
            if (useEmbeddedWebView == WebViewPreference.System)
            {
                requestContext.Logger.Info("Using system browser");
                return new DefaultOsBrowserWebUi(
                    requestContext.ServiceBundle.PlatformProxy,
                    requestContext.Logger,
                    coreUIParent.SystemWebViewOptions);
            }

            if (_isWebView2AvailableFunc(coreUIParent?.WebView2Options?.BrowserExecutableFolder))
            {
                requestContext.Logger.Info("Using WebView2 embedded browser");
                return new WebView2WebUi(coreUIParent, requestContext);
            }

#if DESKTOP
            if (_fallbackToLegacyWebBrowser)
            {
                requestContext.Logger.Info("Using legacy embedded browser");
                return 
                    new Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi.InteractiveWebUI(coreUIParent, requestContext);
            }
#endif
            throw new MsalClientException(
                MsalError.WebView2NotInstalled,
                "The embedded browser needs WebView2 runtime to be installed. If you are an end user of the app, please download and install the WebView2 runtime from https://go.microsoft.com/fwlink/p/?LinkId=2124703 and restart the app." +
                " If you are an app developer, please ensure that your app installs the WebView2 runtime https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution");
        }


        private bool IsWebView2Available(string browserExecutableFolder)
        {
            // if browserExecutableFolder is null, WebView2 SDK will choose global runtime, otherwise, a local runtime.
            string wv2Version = CoreWebView2Environment.GetAvailableBrowserVersionString(browserExecutableFolder);
            return !string.IsNullOrEmpty(wv2Version);
        }
    }
}
