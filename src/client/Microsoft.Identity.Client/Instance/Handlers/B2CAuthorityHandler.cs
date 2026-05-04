// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Handlers
{
    /// <summary>Handler for B2C authorities (path begins with "tfp").</summary>
    internal sealed class B2CAuthorityHandler : IAuthorityHandler
    {
        public AuthorityType AuthorityType => AuthorityType.B2C;

        public bool CanHandle(Uri authorityUri)
        {
            try
            {
                return string.Equals(
                    AuthorityInfo.GetFirstPathSegment(authorityUri),
                    B2CAuthority.Prefix,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public Authority Create(AuthorityInfo authorityInfo)
            => new B2CAuthority(authorityInfo);

        public IAuthorityValidator CreateValidator(RequestContext requestContext)
            => new NullAuthorityValidator();
    }
}
