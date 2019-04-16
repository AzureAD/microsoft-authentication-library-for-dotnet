// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
