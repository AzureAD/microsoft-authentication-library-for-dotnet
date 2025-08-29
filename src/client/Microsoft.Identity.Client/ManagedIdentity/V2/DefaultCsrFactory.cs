// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class DefaultCsrFactory : ICsrFactory
    {
        public (string csrPem, RSA privateKey) Generate(string clientId, string tenantId, CuidInfo cuid)
        {
            return Csr.Generate(clientId, tenantId, cuid);
        }
    }
}
