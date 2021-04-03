using System;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.Identity.Client.Platforms.net5win
{
    internal class Net5WebUiFactory : IWebUIFactory
    {
        private readonly Func<bool> _isWebView2AvailableFunc;

        public Net5WebUiFactory(Func<bool> isWebView2AvailableForTest = null)
        {
            _isWebView2AvailableFunc = isWebView2AvailableForTest ?? IsWebView2Available;
        }

        public bool IsSystemWebViewAvailable => IsUserInteractive;

        public bool IsUserInteractive => DesktopOsHelper.IsUserInteractive();

        public bool IsEmbeddedWebViewAvailable => 
            IsUserInteractive &&
            IsWebView2Available(); // Look for the globally available WebView2 runtime

#if NET5_WIN
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, WebViewPreference useEmbeddedWebView, RequestContext requestContext)
        {
            if (useEmbeddedWebView == WebViewPreference.System)
            {
                requestContext.Logger.Info("Using system browser.");
                return new DefaultOsBrowserWebUi(
                    requestContext.ServiceBundle.PlatformProxy,
                    requestContext.Logger,
                    coreUIParent.SystemWebViewOptions);
            }

            if (_isWebView2AvailableFunc())
            {
                requestContext.Logger.Info("Using WebView2 embedded browser.");
                return new WebView2WebUi(coreUIParent, requestContext);
            }

            requestContext.Logger.Info("Using legacy embedded browser.");
            return new InteractiveWebUI(coreUIParent, requestContext);
        }

        private bool IsWebView2Available()
        {
            try
            {
                string wv2Version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return !string.IsNullOrEmpty(wv2Version);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                return false;
            }
        }
    }
}
