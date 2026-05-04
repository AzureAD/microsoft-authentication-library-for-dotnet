// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Handlers
{
    /// <summary>
    /// Handler for Generic authorities.
    /// Generic authorities are never auto-detected from a URI; they are created explicitly
    /// via <see cref="AuthorityInfo.FromGenericAuthority"/>. CanHandle always returns false,
    /// so this handler is only reachable via <see cref="AuthorityRegistry.GetByType"/>.
    /// </summary>
    internal sealed class GenericAuthorityHandler : IAuthorityHandler
    {
        public AuthorityType AuthorityType => AuthorityType.Generic;

        /// <summary>Generic authorities are never URI-detected; return false always.</summary>
        public bool CanHandle(Uri authorityUri) => false;

        public Authority Create(AuthorityInfo authorityInfo)
            => new GenericAuthority(authorityInfo);

        public IAuthorityValidator CreateValidator(RequestContext requestContext)
            => new NullAuthorityValidator();
    }
}
