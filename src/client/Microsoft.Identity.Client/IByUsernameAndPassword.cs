// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Provides an explicit interface for using Resource Owner Password Grant on Confidential Client.
    /// </summary>
    public interface IByUsernameAndPassword
    {
        /// <summary>
        /// Acquires a token without user interaction using username and password authentication.
        /// This method does not look in the token cache, but stores the result in it. Before calling this method, use other methods 
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> to check the token cache.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="username">Identifier of the user, application requests token on behalf of.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="password">User password as a string.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>
        /// Available only for .NET Framework and .NET Core applications. See <see href="https://aka.ms/msal-net-up">our documentation</see> for details.
        /// </remarks>
        AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder AcquireTokenByUsernamePassword(IEnumerable<string> scopes, string username, string password);
    }
}
