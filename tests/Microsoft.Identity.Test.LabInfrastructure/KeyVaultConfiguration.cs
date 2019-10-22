// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class KeyVaultConfiguration
    {
        /// <summary>
        /// The URL of the Key Vault instance.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The authentication type to use to communicate with the Key Vault.
        /// </summary>
        public LabAccessAuthenticationType AuthType { get; set; }

        /// <summary>
        /// The ID of the test harness client.
        /// </summary>
        /// <remarks>
        /// This should be configured as having access to the Key Vault instance specified at <see cref="Url"/>.
        /// </remarks>
        public string ClientId { get; set; }

        /// <summary>
        /// The thumbprint of the <see cref="System.Security.Cryptography.X509Certificates.X509Certificate2"/> to use when
        /// <see cref="AuthType"/> is <see cref="LabAccessAuthenticationType.ClientCertificate"/>.
        /// </summary>
        public string CertThumbprint { get; set; }

        /// <summary>
        /// Secret value used to access Key Vault
        /// </summary>
        public string KeyVaultSecret { get; set; }
    }


}
