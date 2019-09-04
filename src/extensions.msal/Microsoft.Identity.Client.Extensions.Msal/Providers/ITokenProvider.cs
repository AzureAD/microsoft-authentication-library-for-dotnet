// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    /// ITokenProvider describes the interface for fetching an access token
    /// </summary>
    public interface ITokenProvider
    {
        /// <summary>
        /// Check if the provider is available for use in the current environment
        /// </summary>
        /// <param name="cancel">Cancellation token for early termination of the operation</param>
        /// <returns>True if a credential provider can be built</returns>
        Task<bool> IsAvailableAsync(CancellationToken cancel = default);

        /// <summary>
        /// GetTokenAsync will attempt to fetch a token for a given set of scopes
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="cancel">Cancellation token for early termination of the operation</param>
        /// <returns>An access token as a string</returns>
        Task<IToken> GetTokenAsync(IEnumerable<string> scopes, CancellationToken cancel = default);

        /// <summary>
        /// GetTokenWithResourceUriAsync will attempt to fetch a token for a given resource URI.
        ///
        /// For example, the Azure Resource Manager URI for Azure Public cloud is "https://management.azure.com/".
        /// Note: for the above example, the trailing "/" is significant.
        /// </summary>
        /// <param name="resourceUri">Resource URI requested to access a protected API</param>
        /// <param name="cancel">Cancellation token for early termination of the operation</param>
        /// <returns>An access token as a string</returns>
        Task<IToken> GetTokenWithResourceUriAsync(string resourceUri, CancellationToken cancel = default);
    }
}
