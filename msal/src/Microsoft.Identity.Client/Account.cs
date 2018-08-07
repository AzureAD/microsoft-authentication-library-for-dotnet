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

using Microsoft.Identity.Core;
using System;
using System.Globalization;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains information of a single account. A user can be present in multiple directorie and thus have multiple accounts.
    /// This information is used for token cache lookup and enforcing the user session on STS authorize endpont.
    /// </summary>
    internal sealed class Account: IAccount
    {
        public Account()
        {
        }

        public Account(AccountId homeAccountId, string username, string environment)
        {
            if (homeAccountId == null)
            {
                throw new ArgumentNullException(nameof(homeAccountId));
            }

            Username = username;
            Environment = environment;
            HomeAccountId = homeAccountId;
        }

        public string Username { get; internal set; }

        public string Environment { get; internal set; }

        public AccountId HomeAccountId { get; internal set; }

        public override string ToString()
        {
            return String.Format(
                CultureInfo.CurrentCulture,
                "Account username: {0} environment {1} home account id: {2}",
                Username, Environment, HomeAccountId.ToString());

        }
    }
}