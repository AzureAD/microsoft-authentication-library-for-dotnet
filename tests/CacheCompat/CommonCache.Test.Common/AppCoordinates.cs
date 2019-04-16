// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CommonCache.Test.Common
{
    public class AppCoordinates
    {
        public AppCoordinates(string clientId, string tenant, Uri redirectUri)
        {
            ClientId = clientId;
            Tenant = tenant;
            RedirectUri = redirectUri;
        }

        public string ClientId { get; }
        public string Tenant { get; }
        public string Authority => $"https://login.microsoftonline.com/{Tenant}/";
        public Uri RedirectUri { get; }
    }
}
