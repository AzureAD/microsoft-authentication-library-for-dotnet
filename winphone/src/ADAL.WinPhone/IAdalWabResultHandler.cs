
using Windows.Security.Authentication.Web;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    interface IAdalWabResultHandler
    {
        AdalWebAuthenticationResult Create(WebAuthenticationResult result);
    }
}
