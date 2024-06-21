﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Interface that an MSAL.NET extender can implement to provide their own web UI in public client applications
    /// to sign-in user and have them consented part of the Authorization code flow.
    /// MSAL.NET provides an embedded web view for Windows and Mac, but there are other scenarios not yet supported.
    /// This extensibility point enables them to provide such UI in a secure way
    /// </summary>
    public interface ICustomWebUi
    {
        /// <summary>
        /// Method called by MSAL.NET to delegate the authentication code web with the Secure Token Service (STS)
        /// </summary>
        /// <param name="authorizationUri"> URI computed by MSAL.NET that will let the UI extension
        /// navigate to the STS authorization endpoint in order to sign-in the user and have them consent
        /// </param>
        /// <param name="redirectUri">The redirect URI that was configured. The auth code will be appended to this redirect URI and the browser
        /// will redirect to it.
        /// </param>
        /// <param name="cancellationToken">The cancellation token to which you should respond to.
        /// See <see href="https://learn.microsoft.com/dotnet/standard/parallel-programming/task-cancellation">Task cancellation</see> for details.
        /// </param>
        /// <returns> The URI returned back from the STS authorization endpoint. This URI contains a code=CODE
        /// parameters that MSAL.NET will extract and redeem.
        /// </returns>
        /// <remarks>
        /// The <paramref name="authorizationUri">authorizationUri</paramref> is crafted to
        /// leverage PKCE in order to protect the token from a man in the middle attack.
        /// Only MSAL.NET can redeem the code.
        /// </remarks>
        Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken);
    }
}
