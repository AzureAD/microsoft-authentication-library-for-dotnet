using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Mac
{
    internal class MacUIFactory : IWebUIFactory
    {
        public bool IsSystemWebViewAvailable => true;

        public bool IsDesktopSession => true; 

        public bool IsEmbeddedWebviewAvailable => true;

        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, WebViewPreference webViewPreference, RequestContext requestContext)
        {
            if (webViewPreference == WebViewPreference.System)
            {
                return new DefaultOsBrowserWebUi(
                    requestContext.ServiceBundle.PlatformProxy,
                    requestContext.Logger,
                    coreUIParent.SystemWebViewOptions);
            }

            return new MacEmbeddedWebUI()
            {
                CoreUIParent = coreUIParent,
                RequestContext = requestContext
            };
        }

    }
}
