// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Abstract class containing common API methods and properties. <see cref="T:ManagedIdentityApplication"/>
    /// extend this class. For details see https://aka.ms/msal-net-client-applications.
    /// </summary>
    public partial interface IApplicationBase
    {
        /// <summary>
        /// Details on the configuration of the ClientApplication for debugging purposes.
        /// </summary>
        IAppConfig AppConfig { get; }

        /// <summary>
        /// User token cache. This case holds id tokens, access tokens and refresh tokens for accounts. It's used
        /// and updated silently if needed when calling <see cref="ClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/>
        /// It is updated by each AcquireTokenXXX method, with the exception of <c>AcquireTokenForClient</c> which only uses the application
        /// cache (see <c>IConfidentialClientApplication</c>).
        /// </summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on other platforms.
        /// </remarks>
        ITokenCache UserTokenCache { get; }

   }
}
