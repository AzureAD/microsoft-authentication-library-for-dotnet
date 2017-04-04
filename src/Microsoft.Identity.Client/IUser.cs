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

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains information of a single user. This information is used for token cache lookup and enforcing the user session on STS authorize endpont.
    /// </summary>
    public interface IUser
    {
        /// <summary>
        /// Gets a displayable value in UserPrincipalName (UPN) format. The value can be null.
        /// </summary>
        string DisplayableId { get; }

        /// <summary>
        /// Gets given name of the user if provided by the service. If not, the value is null.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets identity provider if returned by the service. If not, the value is null.
        /// </summary>
        string IdentityProvider { get; }

        /// <summary>
        /// Gets an identifier for the user for the given <see cref="IdentityProvider"/>. Cannot be null.
        /// </summary>
        string Identifier { get; }
   }
}