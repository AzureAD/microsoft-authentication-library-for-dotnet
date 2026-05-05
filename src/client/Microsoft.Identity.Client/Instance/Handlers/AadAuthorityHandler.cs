// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Handlers
{
    /// <summary>
    /// Handler for AAD authorities. Acts as the URI catch-all (CanHandle always returns true)
    /// and must be registered after all more-specific handlers.
    /// </summary>
    internal sealed class AadAuthorityHandler : IAuthorityHandler
    {
        public AuthorityType AuthorityType => AuthorityType.Aad;

        /// <summary>
        /// Catch-all: any URI not claimed by a more-specific handler is treated as AAD.
        /// This handler must be registered last among URI-detectable handlers.
        /// </summary>
        public bool CanHandle(Uri authorityUri, string host, string firstPathSegment) => true;

        public Authority Create(AuthorityInfo authorityInfo)
            => new AadAuthority(authorityInfo);

        public IAuthorityValidator CreateValidator(RequestContext requestContext)
            => new AadAuthorityValidator(requestContext);
    }
}
