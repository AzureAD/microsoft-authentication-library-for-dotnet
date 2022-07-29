// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal class AadAuthorityValidator : IAuthorityValidator
    {
        private readonly RequestContext _requestContext;

        public AadAuthorityValidator(RequestContext requestContext)
        {
            _requestContext = requestContext;
        }

        /// <summary>
        /// AAD performs authority validation by calling the instance metadata endpoint. This is a bit unfortunate, 
        /// because instance metadata is used for aliasing, and authority validation is orthogonal to that. 
        /// MSAL must figure out aliasing even if ValidateAuthority is set to false.
        /// </summary>
        public async Task ValidateAuthorityAsync(
            AuthorityInfo authorityInfo)
        {
            var authorityUri = authorityInfo.CanonicalAuthority;
            bool isKnownEnv = KnownMetadataProvider.IsKnownEnvironment(authorityUri.Host);

            _requestContext.Logger.Info($"Authority validation enabled? {authorityInfo.ValidateAuthority}. ");
            _requestContext.Logger.Info($"Authority validation - is known env? {isKnownEnv}. ");

            if (authorityInfo.ValidateAuthority && !isKnownEnv)
            {
                _requestContext.Logger.Info($"Authority validation is being performed. ");

                // MSAL will throw if the instance discovery URI does not respond with a valid json
                await _requestContext.ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryAsync(
                                             authorityInfo,
                                             _requestContext, 
                                             forceValidation: true).ConfigureAwait(false);
            }
        }
    }
}
