using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.net45;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.Identity.Client.Platforms.net5win
{
    internal class Net5WebUiFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            // no support for hidden ui browser 

            if (!parent.UseEmbeddedWebview)
            {
                requestContext.Logger.Info("Using system browser");
                return new DefaultOsBrowserWebUi(
                    requestContext.ServiceBundle.PlatformProxy,
                    requestContext.Logger,
                    parent.SystemWebViewOptions);
            }

            if (IsWebView2Available())
            {
                requestContext.Logger.Info("Using WebView2 embedded browser");
                return new WebView2WebUi(parent, requestContext);
            }

            requestContext.Logger.Info("Using legacy WebBrowser embedded browser");
            return new InteractiveWebUI(parent, requestContext);
        }

        private bool IsWebView2Available()
        {
            string wv2Version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return !string.IsNullOrEmpty(wv2Version);
        }
    }
}
