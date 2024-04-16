// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http
{
    internal interface IHttpManager
    {
        long LastRequestDurationInMs { get; }

        Task<HttpResponse> SendRequestAsync(
           Uri endpoint,
           Dictionary<string, string> headers,
           HttpContent body,
           HttpMethod method,
           ILoggerAdapter logger,
           bool doNotThrow,
           bool retry,
           X509Certificate2 mtlsCertificate,
           CancellationToken cancellationToken);
    }
}
