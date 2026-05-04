// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Handlers;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Central registry of <see cref="IAuthorityHandler"/> implementations.
    /// Replaces the scattered switch statements and hardcoded string checks for authority type detection,
    /// instantiation, and validator creation.
    /// </summary>
    internal static class AuthorityRegistry
    {
        // Order matters: more-specific URI matchers must precede less-specific ones.
        // CIAM uses host-suffix matching and must appear before AAD, which is the URI catch-all.
        // GenericAuthorityHandler.CanHandle always returns false and is only reachable via GetByType.
        private static readonly IReadOnlyList<IAuthorityHandler> s_handlers = new List<IAuthorityHandler>
        {
            new CiamAuthorityHandler(),
            new AdfsAuthorityHandler(),
            new DstsAuthorityHandler(),
            new B2CAuthorityHandler(),
            new AadAuthorityHandler(),
            new GenericAuthorityHandler(),
        };

        /// <summary>
        /// Detects the correct handler by inspecting a raw authority URI.
        /// URI components are parsed once and passed to each handler to avoid redundant string operations.
        /// Used when parsing a new authority string (will replace GetAuthorityType).
        /// </summary>
        internal static IAuthorityHandler DetectFromUri(Uri authorityUri)
        {
            string host = authorityUri.Host;
            string firstPathSegment;
            try
            {
                firstPathSegment = AuthorityInfo.GetFirstPathSegment(authorityUri);
            }
            catch (InvalidOperationException)
            {
                firstPathSegment = null;
            }

            return s_handlers.FirstOrDefault(h => h.CanHandle(authorityUri, host, firstPathSegment))
                ?? throw new MsalClientException(
                    MsalError.InvalidAuthorityType,
                    $"No authority handler found for URI: {authorityUri}");
        }

        /// <summary>
        /// Looks up the handler for an already-resolved <see cref="AuthorityType"/>.
        /// Used when the type is known (e.g. constructing an Authority from an existing AuthorityInfo).
        /// </summary>
        internal static IAuthorityHandler GetByType(AuthorityType authorityType)
        {
            return s_handlers.FirstOrDefault(h => h.AuthorityType == authorityType)
                ?? throw new MsalClientException(
                    MsalError.InvalidAuthorityType,
                    $"No authority handler registered for type: {authorityType}");
        }

        /// <summary>Creates the concrete Authority subclass for the given AuthorityInfo.</summary>
        internal static Authority Create(AuthorityInfo authorityInfo)
            => GetByType(authorityInfo.AuthorityType).Create(authorityInfo);

        /// <summary>Creates the appropriate validator for the given AuthorityInfo.</summary>
        internal static IAuthorityValidator CreateValidator(AuthorityInfo authorityInfo, RequestContext requestContext)
            => GetByType(authorityInfo.AuthorityType).CreateValidator(requestContext);

        /// <summary>Resolves the authority for a token request, dispatching to the correct handler.</summary>
        internal static Task<Authority> ResolveForRequestAsync(
            Authority configAuthority,
            AuthorityInfo requestAuthorityInfo,
            IAccount account,
            RequestContext requestContext)
            => GetByType(configAuthority.AuthorityInfo.AuthorityType)
                .ResolveForRequestAsync(configAuthority, requestAuthorityInfo, account, requestContext);
    }
}
