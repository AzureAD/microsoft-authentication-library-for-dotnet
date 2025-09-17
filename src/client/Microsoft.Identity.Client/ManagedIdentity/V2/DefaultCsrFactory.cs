// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class DefaultCsrFactory : ICsrFactory
    {
        public (string csrPem, RSA privateKey) Generate(RSA rsa, string clientId, string tenantId, CuidInfo cuid)
        {
            return Csr.Generate(rsa, clientId, tenantId, cuid);
        }
    }
}
