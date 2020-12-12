// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal class AdfsAuthorityValidator : IAuthorityValidator
    {
        private readonly IServiceBundle _serviceBundle;

        public AdfsAuthorityValidator(IServiceBundle serviceBundle)
        {
            _serviceBundle = serviceBundle;
        }

        public async Task ValidateAuthorityAsync(
            AuthorityInfo authorityInfo,
            RequestContext requestContext)
        {
            if (authorityInfo.ValidateAuthority)
            {
                string resource = string.Format(CultureInfo.InvariantCulture, "https://{0}", authorityInfo.Host);
                string webFingerUrl = Constants.FormatAdfsWebFingerUrl(authorityInfo.Host, resource);


                Http.HttpResponse httpResponse = await _serviceBundle.HttpManager.SendGetAsync(
                    new Uri(webFingerUrl), 
                    null, 
                    requestContext.Logger, 
                    cancellationToken: requestContext.UserCancellationToken).ConfigureAwait(false);

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw MsalServiceExceptionFactory.FromHttpResponse(
                        MsalError.InvalidAuthority,
                        MsalErrorMessage.AuthorityValidationFailed,
                        httpResponse);
                }

                AdfsWebFingerResponse wfr = OAuth2Client.CreateResponse<AdfsWebFingerResponse>(httpResponse, requestContext);
                if (wfr.Links.FirstOrDefault(
                        a => a.Rel.Equals(Constants.DefaultRealm, StringComparison.OrdinalIgnoreCase) &&
                             a.Href.Equals(resource)) == null)
                {
                    throw new MsalClientException(
                        MsalError.InvalidAuthority,
                        MsalErrorMessage.InvalidAuthorityOpenId);
                }
            }
        }
    }
}
