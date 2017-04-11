using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace XForms
{
    public interface IAcquireToken
    {
        Task<AuthenticationResult> AcquireTokenAsync(PublicClientApplication app, string[] scopes, UIParent uiParent);

        Task<AuthenticationResult> AcquireTokenAsync(PublicClientApplication app, string[] scopes, string loginHint,
            UIParent uiParent);
    }
}