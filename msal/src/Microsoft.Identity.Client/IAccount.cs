//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------


namespace Microsoft.Identity.Client
{
    /// <summary>
    /// The IAccount interface represents information about a single account. 
    /// The same user can be present in different tenants, that is, a user can have multiple accounts.
    /// An <c>IAccount</c> is returned in <see cref="AuthenticationResult.Account"/>, and can be used as parameters
    /// of PublicClientApplication and ConfidentialClientApplication methods acquiring tokens such as <see cref="ClientApplicationBase.AcquireTokenSilentAsync(System.Collections.Generic.IEnumerable{string}, IAccount)"/>
    /// </summary>
    public interface IAccount
    {
        /// <summary>
        /// Gets a string containing the displayable value in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c>. 
        /// This can be null.        
        /// </summary>
        /// <remarks>This property replaces the <c>DisplayableId</c> property of <c>IUser</c> in previous versions of MSAL.NET</remarks>
        string Username { get; }

        /// <summary>
        /// Gets a string containing the identity provider for this account, e.g. <c>login.microsoftonline.com</c>.
        /// </summary>
        /// <remarks>This property replaces the <c>IdentityProvider</c> property of <c>IUser</c> in previous versions of MSAL.NET
        /// except that IdentityProvider was a URL with information about the tenant (in addition to the cloud environment), whereas Environement is only the <see cref="System.Uri.Host"/></remarks>
        string Environment { get; }

        /// <summary>
        /// AccountId of the home account for the user. This uniquely identifies the user across AAD tenants. 
        /// </summary>
        /// <remarks>Can be null</remarks>
        AccountId HomeAccountId { get; }
   }
}