// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Features.KeyMaterial
{
    /// <summary>
    /// Factory clas for creating and managing instances of KeyMaterialManager
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    internal static class KeyMaterialProviderFactory
    {
        private static KeyMaterialManager s_keyMaterialManager;
        private static readonly object s_lock = new();

        public static KeyMaterialManager GetKeyMaterial(ILoggerAdapter logger)
        {
            lock (s_lock)
            {
                s_keyMaterialManager = new KeyMaterialManager(logger);
                return s_keyMaterialManager;
            }
        }
    }
}
