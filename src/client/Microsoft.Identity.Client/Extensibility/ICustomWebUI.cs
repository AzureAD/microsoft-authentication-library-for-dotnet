// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <remarks>
    /// <format type="text/markdown">
    /// <![CDATA[
    /// > [!CAUTION]
    /// > **ICustomWebUi is not recommended for production use due to security risks and current service limitations, and is on a deprecation path.**
    /// >
    /// > This pattern introduces security risks and is not supported by Entra ID cloud services. Using native client redirect URIs (like `https://login.microsoftonline.com/common/oauth2/nativeclient`) with custom web UI implementations typically requires users to manually copy the authorization code from the URL—an anti-pattern most commonly seen with the `nativeclient` URI. This pattern will not work in most configurations and poses security risks.
    /// >
    /// > - **Recommended Alternatives**: 
    /// >   - **Use [Broker authentication (WAM)](https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam)** for Windows 10+ applications - provides the best security and user experience
    /// >   - **Use embedded browser flow** as described in [Using web browsers](https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/using-web-browsers)
    /// ]]>
    /// </format>
    /// </remarks>
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
