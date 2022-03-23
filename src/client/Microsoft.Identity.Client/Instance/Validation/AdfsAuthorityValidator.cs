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
        private readonly RequestContext _requestContext;

        public AdfsAuthorityValidator(RequestContext requestContext)
        {
            _requestContext = requestContext;
        }

        public async Task ValidateAuthorityAsync(
            AuthorityInfo authorityInfo)
        {
            if (authorityInfo.ValidateAuthority)
            {
                string resource = string.Format(CultureInfo.InvariantCulture, "https://{0}", authorityInfo.Host);
                string webFingerUrl = Constants.FormatAdfsWebFingerUrl(authorityInfo.Host, resource);

                Http.HttpResponse httpResponse = await _requestContext.ServiceBundle.HttpManager.SendGetAsync(
                    new Uri(webFingerUrl), 
                    null,
                    _requestContext.Logger, 
                    cancellationToken: _requestContext.UserCancellationToken).ConfigureAwait(false);

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw MsalServiceExceptionFactory.FromHttpResponse(
                        MsalError.InvalidAuthority,
                        MsalErrorMessage.AuthorityValidationFailed,
                        httpResponse);
                }

                AdfsWebFingerResponse wfr = OAuth2Client.CreateResponse<AdfsWebFingerResponse>(httpResponse, _requestContext);
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
