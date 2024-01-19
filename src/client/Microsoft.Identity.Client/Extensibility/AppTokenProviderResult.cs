// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Token result from external app token provider
    /// </summary>
    /// <remarks>
    /// This is part of an extensibility mechanism designed to be used by Azure SDK in order to 
    /// enhance managed identity support.
    /// </remarks>
    public class AppTokenProviderResult
    {
        /// <summary>
        /// The actual token, usually in JWT format
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Expiration of token 
        /// </summary>
        /// <remarks>Mandatory</remarks>
        public long ExpiresInSeconds { get; set; }

        /// <summary>
        /// When the token should be refreshed.
        /// </summary>
        /// <remarks>If not set, MSAL will set it to half of the expiry time if that time is longer than 2 hours.</remarks>
        public long? RefreshInSeconds { get; set; }
    }
}
