// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Handlers
{
    /// <summary>
    /// Handler for CIAM authorities (host ends with ".ciamlogin.com").
    /// Must be registered before <see cref="AadAuthorityHandler"/>.
    /// </summary>
    internal sealed class CiamAuthorityHandler : IAuthorityHandler
    {
        public AuthorityType AuthorityType => AuthorityType.Ciam;

        public bool CanHandle(Uri authorityUri, string host, string firstPathSegment)
            => host.EndsWith(Constants.CiamAuthorityHostSuffix, StringComparison.OrdinalIgnoreCase);

        public Authority Create(AuthorityInfo authorityInfo)
            => new CiamAuthority(authorityInfo);

        public IAuthorityValidator CreateValidator(RequestContext requestContext)
            => new NullAuthorityValidator();
    }
}
