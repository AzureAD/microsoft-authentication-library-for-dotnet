// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Extends <see cref="IAuthenticationOperation2"/> with a lifecycle hook so MSAL can
    /// pass runtime context (e.g., the mTLS certificate selected for the current request)
    /// to a custom authentication operation. Enables composition of schemes such as
    /// CDT + mTLS PoP without MSAL having to replace the operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This callback fires on every <c>ExecuteAsync</c> invocation, including when a valid
    /// token is served from the cache. Implementations should avoid expensive work that is
    /// unnecessary on cache hits.
    /// </para>
    /// <para>
    /// <b>Thread safety:</b> If the same <see cref="IAuthenticationOperation3"/> instance
    /// is shared across concurrent <c>ExecuteAsync</c> calls, <see cref="AfterCredentialEvaluationAsync"/>
    /// will be invoked concurrently. Implementations must either be thread-safe or create
    /// a fresh instance per <c>ExecuteAsync</c> call (recommended).
    /// </para>
    /// <para>
    /// <b>Scope:</b> This hook fires only on confidential client token requests.
    /// Managed identity flows do not invoke it.
    /// </para>
    /// <para>
    /// <b>Exceptions:</b> Exceptions thrown from this callback will propagate
    /// to the caller of <c>ExecuteAsync</c> without wrapping.
    /// </para>
    /// </remarks>
    public interface IAuthenticationOperation3 : IAuthenticationOperation2
    {
        /// <summary>
        /// MSAL invokes this once per token request, after it has evaluated the
        /// credentials that will be used. The operation reads what it needs from
        /// <paramref name="context"/> and configures itself for the upcoming
        /// <see cref="IAuthenticationOperation.FormatResult(AuthenticationResult)"/> call.
        /// </summary>
        /// <param name="context">
        /// Runtime state owned by MSAL. Never <c>null</c>. Must not be retained past this call.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AfterCredentialEvaluationAsync(TokenAcquisitionContext context, CancellationToken cancellationToken = default);
    }
}
