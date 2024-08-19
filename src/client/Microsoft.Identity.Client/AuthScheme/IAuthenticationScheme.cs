﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Extensiblity interface Used to modify the experience depending on the type of token asked. 
    /// </summary>
    public interface IAuthenticationScheme 
    {
        //TODO bogavril - abstract class instead of interface?
        //TODO bogavril - replace legacy POP impl in Id.Web with this and hard deprecate WithKeyId extensiblity point

        /// <summary>
        /// Value to log to HTTP telemetry.
        /// </summary>
        TokenType TelemetryTokenType { get; }  // TODO: bogavril - enums are not good for extensiblity, consider using INT

        /// <summary>
        /// Prefix for the HTTP header that has the token. E.g. "Bearer" or "POP"
        /// </summary>
        string AuthorizationHeaderPrefix { get; }

        /// <summary>
        /// Extra parameters that are added to the request to the /token endpoint. 
        /// </summary>
        /// <returns>Name and values of params</returns>
        IReadOnlyDictionary<string, string> GetTokenRequestParams();

        /// <summary>
        /// Key ID of the public / private key pair used by the encryption algorithm, if any. 
        /// Tokens obtained by authentication schemes that use this are bound to the KeyId, i.e. 
        /// if a different kid is presented, the access token cannot be used.
        /// </summary>
        string KeyId { get; }

        /// <summary>
        /// Creates the access token that goes into an Authorization HTTP header. 
        /// </summary>
        void FormatResult(AuthenticationResult authenticationResult); 

        /// <summary>
        /// Expected to match the token_type parameter returned by ESTS. Used to disambiguate
        /// between ATs of different types (e.g. Bearer and PoP) when loading from cache etc.
        /// </summary>
        string AccessTokenType { get; }
    }
}
