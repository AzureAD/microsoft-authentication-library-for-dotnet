// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Handlers
{
    /// <summary>Handler for ADFS authorities (path begins with "adfs").</summary>
    internal sealed class AdfsAuthorityHandler : IAuthorityHandler
    {
        private const string AdfsPathSegment = "adfs";

        public AuthorityType AuthorityType => AuthorityType.Adfs;

        public bool CanHandle(Uri authorityUri, string host, string firstPathSegment)
            => string.Equals(firstPathSegment, AdfsPathSegment, StringComparison.OrdinalIgnoreCase);

        public Authority Create(AuthorityInfo authorityInfo)
            => new AdfsAuthority(authorityInfo);

        public IAuthorityValidator CreateValidator(RequestContext requestContext)
            => new AdfsAuthorityValidator(requestContext);
    }
}
