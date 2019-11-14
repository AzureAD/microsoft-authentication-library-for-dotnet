// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CommonCache.Test.Common
{
    public class LabUserData
    {
        public LabUserData(string upn, string password, string clientId, string tenantId)
        {
            Upn = upn ?? throw new ArgumentNullException(nameof(upn));
            Password = password ?? throw new ArgumentNullException(nameof(clientId));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        }

        public string Upn { get; set; }
        public string Password { get; set; }

        public string ClientId { get; }
        public string TenantId { get; }
        public string Authority => $"https://login.microsoftonline.com/{TenantId}/";
    }
}
