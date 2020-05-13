// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal class AadAuthorityValidator : IAuthorityValidator
    {
        private readonly IServiceBundle _serviceBundle;

        public AadAuthorityValidator(IServiceBundle serviceBundle)
        {
            _serviceBundle = serviceBundle;
        }

        /// <summary>
        /// AAD performs authority validation by calling the instance metadata endpoint. This is a bit unfortunate, 
        /// because instance metadata is used for aliasing, and authority validation is orthogonal to that. 
        /// MSAL must figure out aliasing even if ValidateAuthority is set to false.
        /// </summary>
        public async Task ValidateAuthorityAsync(
            AuthorityInfo authorityInfo,
            RequestContext requestContext)
        {
            var authorityUri = new Uri(authorityInfo.CanonicalAuthority);
            if (authorityInfo.ValidateAuthority && !KnownMetadataProvider.IsKnownEnvironment(authorityUri.Host))
            {
                // MSAL will throw if the instance discovery URI does not respond with a valid json
                await _serviceBundle.InstanceDiscoveryManager.GetMetadataEntryAsync(
                                             authorityInfo.CanonicalAuthority,
                                             requestContext).ConfigureAwait(false);
            }
        }
    }
}
