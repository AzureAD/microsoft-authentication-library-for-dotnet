// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Cache
{
    internal class AdalUserForMsalEntry
    {
        public AdalUserForMsalEntry(string clientId, string authority, string clientInfo, AdalUserInfo userInfo)
        {
            ClientId = Guard.AgainstNull(clientId);
            Authority = authority;
            ClientInfo = clientInfo;
            UserInfo = Guard.AgainstNull(userInfo);
        }

        public string ClientId { get; }
        public string Authority { get; }
        public string ClientInfo { get; } // optional, ADAL v3 doesn't have this
        public AdalUserInfo UserInfo { get; }
    }
}
