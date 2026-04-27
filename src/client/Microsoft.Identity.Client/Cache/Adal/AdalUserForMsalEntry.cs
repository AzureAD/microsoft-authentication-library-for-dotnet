// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Cache
{
    internal class AdalUserForMsalEntry(string clientId, string authority, string clientInfo, AdalUserInfo userInfo)
    {
        public string ClientId { get; } = clientId ?? throw new ArgumentNullException(nameof(clientId));
        public string Authority { get; } = authority;
        public string ClientInfo { get; } = clientInfo;
        public AdalUserInfo UserInfo { get; } = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
    }
}
