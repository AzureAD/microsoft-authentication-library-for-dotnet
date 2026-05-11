// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Extends <see cref="IAuthenticationOperation"/> with a lifecycle hook so MSAL can
    /// pass runtime context (e.g., the mTLS certificate selected for the current request)
    /// to a custom authentication operation. Enables composition of schemes such as
    /// CDT + mTLS PoP without MSAL having to replace the operation.
    /// </summary>
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
        void AfterCredentialEvaluation(TokenAcquisitionContext context);
    }
}
