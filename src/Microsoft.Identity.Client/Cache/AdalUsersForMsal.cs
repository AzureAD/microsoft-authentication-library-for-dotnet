// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Cache
{
    internal class AdalUsersForMsal
    {
        private readonly IEnumerable<AdalUserForMsalEntry> _userEntries;

        public AdalUsersForMsal(IEnumerable<AdalUserForMsalEntry> userEntries)
        {
            _userEntries = userEntries ?? throw new ArgumentNullException(nameof(userEntries));
        }

        public IDictionary<string, AdalUserInfo> GetUsersWithClientInfo(IEnumerable<string> envAliases)
        {
            return _userEntries
                .Where(u => !string.IsNullOrEmpty(u.Authority) &&
                            !string.IsNullOrEmpty(u.ClientInfo) &&
                            (envAliases?.ContainsOrdinalIgnoreCase(Authority.GetEnviroment(u.Authority)) ?? true))
                            .ToLookup(u => u.ClientInfo, u => u.UserInfo)
                            .ToDictionary(group => group.Key, group => group.First());

        }

        public IEnumerable<AdalUserInfo> GetUsersWithoutClientInfo(IEnumerable<string> envAliases)
        {
            return _userEntries
                .Where(u => !string.IsNullOrEmpty(u.Authority) &&
                            string.IsNullOrEmpty(u.ClientInfo) &&
                            (envAliases?.ContainsOrdinalIgnoreCase(Authority.GetEnviroment(u.Authority)) ?? true))
                .Select(u => u.UserInfo);
        }

        public ISet<string> GetAdalUserEnviroments()
        {
            var envList = _userEntries
                .Where(u => !string.IsNullOrEmpty(u.Authority))
                .Select(u => Authority.GetEnviroment(u.Authority));

            return new HashSet<string>(envList, StringComparer.OrdinalIgnoreCase);
        }
    }
}
