using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace XForms
{
    class AcquireToken : IAcquireToken
    {
        public Task<AuthenticationResult> AcquireTokenAsync(PublicClientApplication app, string[] scopes, UIParent uiParent)
        {
            return app.AcquireTokenAsync(scopes);
        }

        public Task<AuthenticationResult> AcquireTokenAsync(PublicClientApplication app, string[] scopes, string loginHint, UIParent uiParent)
        {
            return app.AcquireTokenAsync(scopes, loginHint);
        }
    }
}
