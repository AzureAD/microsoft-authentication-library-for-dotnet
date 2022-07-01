using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{

    /// <summary>
    /// Wrapper class to enable testing, since WebTokenRequestResult object doesn't have any ctor
    /// and interface is not accessible.
    /// </summary>
#if NET5_WIN
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
    internal class WebTokenRequestResultWrapper : IWebTokenRequestResultWrapper
    {

        public WebTokenRequestResultWrapper(WebTokenRequestResult webTokenRequestResult)
        {
            ResponseData = webTokenRequestResult.ResponseData;
            ResponseError = webTokenRequestResult.ResponseError;
            ResponseStatus = webTokenRequestResult.ResponseStatus;
        }

        //
        // Summary:
        //     Gets the response data from the web token provider.
        //
        // Returns:
        //     The response from the web token provider.
        public IReadOnlyList<WebTokenResponse> ResponseData { get; }
        //
        // Summary:
        //     Gets the error returned by the web provider, if any.
        //
        // Returns:
        //     The error returned by the web provider.
        public WebProviderError ResponseError { get; }
        //
        // Summary:
        //     Gets the status of the request.
        //
        // Returns:
        //     The status of the request.
        public WebTokenRequestStatus ResponseStatus { get; }

    }
}
