// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Encapsulates authority-type-specific logic: URI detection, object creation, and validation.
    /// Implementations are registered in <see cref="AuthorityRegistry"/> in priority order.
    /// </summary>
    internal interface IAuthorityHandler
    {
        /// <summary>The authority type this handler manages.</summary>
        AuthorityType AuthorityType { get; }

        /// <summary>
        /// Returns true if this handler can process the given authority URI.
        /// Handlers are evaluated in registration order; the first match wins.
        /// </summary>
        bool CanHandle(Uri authorityUri);

        /// <summary>Creates the concrete <see cref="Authority"/> subclass for the given info.</summary>
        Authority Create(AuthorityInfo authorityInfo);

        /// <summary>Creates the appropriate validator for this authority type.</summary>
        IAuthorityValidator CreateValidator(RequestContext requestContext);
    }
}
