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
    /// <remarks>
    /// mTLS binding info constructor.
    /// </remarks>
    /// <param name="certificate"></param>
    /// <param name="endpoint"></param>
    /// <param name="clientId"></param>
    /// <exception cref="ArgumentNullException"></exception>
    internal sealed class MtlsBindingInfo(
        X509Certificate2 certificate,
        string endpoint,
        string clientId)
    {
        public X509Certificate2 Certificate { get; } = certificate ?? throw new ArgumentNullException(nameof(certificate));
        public string Endpoint { get; } = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        public string ClientId { get; } = clientId ?? throw new ArgumentNullException(nameof(clientId));
    }
}
