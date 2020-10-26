// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Instance
{
    internal static class AdfsUpnHelper
    {
        public static string GetDomainFromUpn(string upn)
        {
            if (!upn.Contains("@"))
            {
                throw new ArgumentException("userPrincipalName does not contain @ character.");
            }

            return upn.Split('@')[1];
        }
    }
}
