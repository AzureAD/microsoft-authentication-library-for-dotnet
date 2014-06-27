using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class MockAdalWabResultHandler : IAdalWabResultHandler
    {
        private readonly AdalWebAuthenticationResult mockInstance;

        public MockAdalWabResultHandler(string responseData, uint responseErrorDetail, WebAuthenticationStatus responseStatus)
        {
            mockInstance = new AdalWebAuthenticationResult();
            mockInstance.ResponseData = responseData;
            mockInstance.ResponseErrorDetail = responseErrorDetail;
            mockInstance.ResponseStatus = responseStatus;
        }
        public AdalWebAuthenticationResult Create(WebAuthenticationResult result)
        {
            //ignore the result passed. return pre constructed object instead.
            return mockInstance;
        }
    }
}
