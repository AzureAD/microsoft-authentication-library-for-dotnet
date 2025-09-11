// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.Internal
{
    internal sealed class MtlsPopRequest
    {
        public string ClientId { get; set; }
        public Uri AttestationEndpoint { get; set; }
        public SafeNCryptKeyHandle KeyHandle { get; set; }
    }

    internal sealed class MtlsPopResponse
    {
        public string AttestationToken { get; set; }
    }
}
