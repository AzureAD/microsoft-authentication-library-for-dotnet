// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Rest api
    /// </summary>
    public interface IDownstreamRestApi
    {
        Task<HttpResponseMessage> CallGetApiAsync(Uri targetUrl, CancellationToken cancellationToken);

        // others follow for POST, PUT, DELETE etc. Ignore for now.
        Task<HttpResponseMessage> CallPostApiAsync(Uri targetUrl, FormUrlEncodedContent content, CancellationToken cancellationToken);
    }
}
