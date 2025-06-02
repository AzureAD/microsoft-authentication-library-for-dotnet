// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using static Microsoft.Identity.Client.Http.Retry.IRetryPolicyFactory;

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
                var resource = $"https://{authorityInfo.Host}";
                string webFingerUrl = Constants.FormatAdfsWebFingerUrl(authorityInfo.Host, resource);

                IRetryPolicyFactory retryPolicyFactory = _requestContext.ServiceBundle.Config.RetryPolicyFactory;
                IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.STS);

                Http.HttpResponse httpResponse = await _requestContext.ServiceBundle.HttpManager.SendRequestAsync(
                    new Uri(webFingerUrl),
                    null,
                    body: null,
                    method: System.Net.Http.HttpMethod.Get,
                    logger: _requestContext.Logger,
                    doNotThrow: false,
                    mtlsCertificate: null,
                    validateServerCertificate: null,
                    cancellationToken: _requestContext.UserCancellationToken,
                    retryPolicy: retryPolicy
                    )
                    .ConfigureAwait(false);

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    _requestContext.Logger.Error($"Authority validation failed because the configured authority is invalid. Authority: {authorityInfo.CanonicalAuthority}");
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
                    _requestContext.Logger.Error($"Authority validation failed because the configured authority is invalid. Authority: {authorityInfo.CanonicalAuthority}");
                    throw new MsalClientException(
                        MsalError.InvalidAuthority,
                        MsalErrorMessage.InvalidAuthorityOpenId);
                }
            }
        }
    }
}
