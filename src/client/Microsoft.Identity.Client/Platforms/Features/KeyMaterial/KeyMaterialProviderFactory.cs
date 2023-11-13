// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Features.KeyMaterial
{
    /// <summary>
    /// Class to store crypto key information for a Managed Identity supported Azure resource.
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    internal static class KeyMaterialProviderFactory
    {
        private static KeyMaterialManager s_keyMaterialManager;
        private static readonly object s_lock = new();

        public static KeyMaterialManager GetKeyMaterial()
        {
            lock (s_lock)
            {
                if (s_keyMaterialManager != null)
                {
                    return s_keyMaterialManager;
                }

                s_keyMaterialManager = new KeyMaterialManager();
                return s_keyMaterialManager;
            }
        }
    }
}
