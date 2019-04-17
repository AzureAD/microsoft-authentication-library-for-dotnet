// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Schema;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class ReadCredentialsResponse
    {
        public ReadCredentialsResponse(IEnumerable<Credential> credentials, OperationStatus status)
        {
            // todo: clone
            Credentials = credentials;
            Status = status;
        }

        public IEnumerable<Credential> Credentials { get; }
        public OperationStatus Status { get; }
    }
}
