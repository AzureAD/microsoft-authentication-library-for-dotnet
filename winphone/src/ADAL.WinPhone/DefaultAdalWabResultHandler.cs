using Windows.Security.Authentication.Web;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class DefaultAdalWabResultHandler : IAdalWabResultHandler
    {
        public AdalWebAuthenticationResult Create(WebAuthenticationResult result)
        {
            AdalWebAuthenticationResult returnValue = new AdalWebAuthenticationResult();
            returnValue.ResponseData = result.ResponseData;
            returnValue.ResponseErrorDetail = result.ResponseErrorDetail;
            returnValue.ResponseStatus = result.ResponseStatus;
            return returnValue;
        }
    }
}
