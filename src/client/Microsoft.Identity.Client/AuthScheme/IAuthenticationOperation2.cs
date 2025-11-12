// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// This is an extensibility API and should only be used by SDKs.
    /// Enhanced version of IAuthenticationOperation that supports asynchronous token formatting.
    /// Used to modify the experience depending on the type of token asked with async capabilities.
    /// </summary>
    public interface IAuthenticationOperation2 : IAuthenticationOperation
    {
        /// <summary>
        /// Will be invoked instead of IAuthenticationOperation.FormatResult
        /// </summary>
        Task FormatResultAsync(AuthenticationResult authenticationResult, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether the cached token is still valid.
        /// </summary>
        /// <param name="cachedTokenData">Data used to determine if token is valid</param>
        /// <returns></returns>
        Task<bool> ValidateCachedTokenAsync(MsalCacheValidationData cachedTokenData);
    }
}
