using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.Identity.Client.Platforms.net5win
{
    internal class Net5WebUiFactory : IWebUIFactory
    {
        private readonly Func<string, bool> _isWebView2AvailableFunc;

        public Net5WebUiFactory(Func<string, bool> isWebView2AvailableForTest = null)
        {
            _isWebView2AvailableFunc = isWebView2AvailableForTest ?? IsWebView2Available;
        }

        public bool IsSystemWebViewAvailable => IsUserInteractive;

        public bool IsUserInteractive => DesktopOsHelper.IsUserInteractive();

        public bool IsEmbeddedWebViewAvailable => 
            IsUserInteractive &&
            IsWebView2Available(null); // Look for the globally available WebView2 runtime

#if NET5_WIN
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, WebViewPreference useEmbeddedWebView, RequestContext requestContext)
        {
            if (useEmbeddedWebView == WebViewPreference.System)
            {
                requestContext.Logger.Info("Using system browser");
                return new DefaultOsBrowserWebUi(
                    requestContext.ServiceBundle.PlatformProxy,
                    requestContext.Logger,
                    coreUIParent.SystemWebViewOptions);
            }


            if (_isWebView2AvailableFunc(coreUIParent?.EmbeddedWebviewOptions?.WebView2BrowserExecutableFolder))
            {
                requestContext.Logger.Info("Using WebView2 embedded browser");
                return new WebView2WebUi(coreUIParent, requestContext);
            }

            throw new MsalClientException(
                MsalError.WebView2NotInstalled,
                "The embedded browser needs WebView2 runtime to be installed. If you are an end user of the app, please download and install the WebView2 runtime from https://go.microsoft.com/fwlink/p/?LinkId=2124703 and restart the app." +
                " If you are an app developer, please ensure that your app installs the WebView2 runtime https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution");
        }

        private bool IsWebView2Available(string browserExecutableFolder)
        {
            try
            {
                string wv2Version = CoreWebView2Environment.GetAvailableBrowserVersionString(browserExecutableFolder);
                return !string.IsNullOrEmpty(wv2Version);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                return false;
            }
        }
    }
}

