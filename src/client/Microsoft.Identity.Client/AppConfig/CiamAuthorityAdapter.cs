// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client
{
    internal class CiamAuthorityAdapter
    {
        public CiamAuthorityAdapter(string authority)
        {
            TransformAuthority(authority);
        }

        public CiamAuthorityAdapter(string instance, string tenant)
        {
            instance = instance.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(tenant))
            {
                TransformAuthority(instance + '/' + tenant);
            }
            else
            {
                TransformedInstance = instance;
                TransformedTenant = tenant;
                TransformedAuthority = instance + '/' + tenant;
            }
        }

        public string TransformedAuthority { get; private set; }

        public string TransformedTenant { get; private set; }

        public string TransformedInstance { get; private set; }

        public string TransformedMetadata { get; private set; } = "{\"api-version\": \"1.1\",\"metadata\": [{\"preferred_network\": \"login.windows.net\",\"preferred_cache\": \"login.windows.net\",\"aliases\": [\"login.windows.net\",\"login.ciamlogin.com\"]}]}";

        private void TransformAuthority(string ciamAuthority)
        {
            Uri uriCiam = new(ciamAuthority);
            string host = uriCiam.Host + uriCiam.AbsolutePath;
            if (string.Equals(uriCiam.AbsolutePath, "/"))
            {
                string ciamTenant = host.Substring(0, host.IndexOf(".ciamlogin.com", StringComparison.OrdinalIgnoreCase));
                TransformedInstance = "https://login.ciamlogin.com/";
                TransformedTenant = ciamTenant + ".onmicrosoft.com";
                TransformedAuthority = TransformedInstance + TransformedTenant;
            }
            else
            {
                TransformedAuthority = ciamAuthority;
                TransformedInstance = "https://" + uriCiam.Host;
                TransformedTenant = uriCiam.AbsolutePath.Trim().TrimStart('/');
            }
        }
    }
}
