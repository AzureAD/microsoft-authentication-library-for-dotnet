using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Mac
{
    internal class MacUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            if (!parent.UseEmbeddedWebview)
            {
                return new DefaultOsBrowserWebUi(
                    requestContext.ServiceBundle.PlatformProxy,
                    requestContext.Logger,
                    parent.SystemWebViewOptions);
            }

            return new MacEmbeddedWebUI()
            {
                CoreUIParent = parent,
                RequestContext = requestContext
            };
        }
    }
}
