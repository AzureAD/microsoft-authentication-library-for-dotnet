// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class CsrRequest
    {
        public string Pem { get; }

        public CsrRequest(string pem)
        {
            Pem = pem ?? throw new ArgumentNullException(nameof(pem));
        }

        /// <summary>
        /// Generates a CSR for the given client, tenant, and CUID info.
        /// </summary>
        /// <param name="clientId">Managed Identity client_id.</param>
        /// <param name="tenantId">AAD tenant_id.</param>
        /// <param name="cuid">CuidInfo object containing VMID and VMSSID.</param>
        /// <returns>CsrRequest containing the PEM CSR.</returns>
        public static CsrRequest Generate(string clientId, string tenantId, CuidInfo cuid)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("clientId must not be null or empty.", nameof(clientId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId must not be null or empty.", nameof(tenantId));
            if (cuid == null)
                throw new ArgumentNullException(nameof(cuid));
            if (string.IsNullOrWhiteSpace(cuid.Vmid))
                throw new ArgumentException("cuid.Vmid must not be null or empty.", nameof(cuid.Vmid));
            if (string.IsNullOrWhiteSpace(cuid.Vmssid))
                throw new ArgumentException("cuid.Vmssid must not be null or empty.", nameof(cuid.Vmssid));

            // TODO: Implement the actual CSR generation logic.
            return new CsrRequest("pem");
        }
    }
}
