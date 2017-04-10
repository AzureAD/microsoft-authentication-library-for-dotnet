using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace XForms
{
    public interface IAcquireToken
    {
        Task<IAuthenticationResult> AcquireTokenAsync(PublicClientApplication app, string[] scopes, UIParent uiParent);

        Task<IAuthenticationResult> AcquireTokenAsync(PublicClientApplication app, string[] scopes, string loginHint,
            UIParent uiParent);
    }
}