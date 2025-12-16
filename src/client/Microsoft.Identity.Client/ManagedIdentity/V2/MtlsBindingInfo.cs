// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// mTLS binding information: certificate, endpoint, client ID.
    /// </summary>
    internal sealed class MtlsBindingInfo
    {
        /// <summary>
        /// mTLS binding info constructor.
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="endpoint"></param>
        /// <param name="clientId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MtlsBindingInfo(
            X509Certificate2 certificate,
            string endpoint,
            string clientId)
        {
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        public X509Certificate2 Certificate { get; }
        public string Endpoint { get; }
        public string ClientId { get; }
    }
}
