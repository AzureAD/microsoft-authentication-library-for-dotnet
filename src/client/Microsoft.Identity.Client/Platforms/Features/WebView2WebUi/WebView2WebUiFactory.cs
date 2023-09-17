// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.Identity.Client.Platforms.Features.WebView2WebUi
{
    internal class WebView2WebUiFactory : IWebUIFactory
    {
        private readonly Func<bool> _isWebView2AvailableFunc;

        public WebView2WebUiFactory(Func<bool> isWebView2AvailableForTest = null)
        {
            _isWebView2AvailableFunc = isWebView2AvailableForTest ?? IsWebView2Available;
        }

        public bool IsSystemWebViewAvailable => IsUserInteractive;

        public bool IsUserInteractive => DesktopOsHelper.IsUserInteractive();

        public bool IsEmbeddedWebViewAvailable =>
            IsUserInteractive &&
            IsWebView2Available(); // Look for the globally available WebView2 runtime

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

            AuthorityType authorityType = requestContext.ServiceBundle.Config.Authority.AuthorityInfo.AuthorityType;

            if (authorityType == AuthorityType.Aad)
            {
                requestContext.Logger.Info($"Using WebView1 embedded browser because the authority is {authorityType}. WebView2 does not provide SSO.");
                return new InteractiveWebUI(coreUIParent, requestContext);
            }

            if (!_isWebView2AvailableFunc())
            {
                requestContext.Logger.Info("Using WebView1 embedded browser because WebView2 is not available.");
                return new InteractiveWebUI(coreUIParent, requestContext);
            }

            requestContext.Logger.Info("Using WebView2 embedded browser.");
            return new WebView2WebUi(coreUIParent, requestContext);
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
            catch (Exception ex) when (ex is BadImageFormatException || ex is DllNotFoundException)
            {
                throw new MsalClientException(MsalError.WebView2LoaderNotFound, MsalErrorMessage.WebView2LoaderNotFound, ex);
            }
        }
    }
}
