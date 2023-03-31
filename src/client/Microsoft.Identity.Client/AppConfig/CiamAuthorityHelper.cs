// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    internal class CiamAuthorityHelper
    {
        public static readonly string CiamMetadata = "{\"api-version\": \"1.1\",\"metadata\": [{\"preferred_network\": \"login.windows.net\",\"preferred_cache\": \"login.windows.net\",\"aliases\": [\"login.windows.net\",\"login.ciamlogin.com\"]}]}";
        public CiamAuthorityHelper(Uri authority)
        {
            TransformAuthority(authority);
        }

        public CiamAuthorityHelper(string instance, string tenant)
        {
            instance = instance.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(tenant))
            {
                TransformAuthority(new Uri(instance + '/' + tenant));
            }
            else
            {
                TransformedInstance = instance;
                TransformedTenant = tenant;
                TransformedAuthority = new Uri(instance + '/' + tenant);
            }
        }

        public Uri TransformedAuthority { get; private set; }

        public string TransformedTenant { get; private set; }

        public string TransformedInstance { get; private set; }

        

        private void TransformAuthority(Uri ciamAuthority)
        {
            string host = ciamAuthority.Host + ciamAuthority.AbsolutePath;
            if (string.Equals(ciamAuthority.AbsolutePath, "/"))
            {
                string ciamTenant = host.Substring(0, host.IndexOf(".ciamlogin.com", StringComparison.OrdinalIgnoreCase));
                TransformedInstance = "https://login.ciamlogin.com/";
                TransformedTenant = ciamTenant + ".onmicrosoft.com";
                TransformedAuthority = new Uri (TransformedInstance + TransformedTenant);
            }
            else
            {
                TransformedAuthority = ciamAuthority;
                TransformedInstance = "https://" + ciamAuthority.Host;
                TransformedTenant = ciamAuthority.AbsolutePath.Trim().TrimStart('/');
            }
        }
    }
}
