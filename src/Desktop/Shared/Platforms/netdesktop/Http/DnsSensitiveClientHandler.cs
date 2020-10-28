// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if DESKTOP

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.Platforms.net45.Http
{
    internal class DnsSensitiveClientHandler : DelegatingHandler
    {
        private readonly ConcurrentDictionary<EndpointCacheKey, bool> _endpoinsWithTcp =
            new ConcurrentDictionary<EndpointCacheKey, bool>();

        public DnsSensitiveClientHandler()
        {
            base.InnerHandler = new HttpClientHandler() { UseDefaultCredentials = true };
        }

        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AddConnectionLeaseTimeout(request.RequestUri);
            return await base.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Based on https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs and 
        /// the associated blog post https://nima-ara-blog.azurewebsites.net/beware-of-the-net-httpclient/
        /// </summary>
        private void AddConnectionLeaseTimeout(Uri endpoint)
        {
            if (!endpoint.IsAbsoluteUri)
            {
                return;
            }

            var key = new EndpointCacheKey(endpoint);
            if (!_endpoinsWithTcp.TryGetValue(key, out var _))
            {
                // ServicePointManager is responsible for managing different properties of a TCP connection 
                // and one of such properties is the ConnectionLeaseTimeout. 
                // This specifies how long (in ms) the TCP socket can stay open. 
                // By default the value of this property is set to -1 resulting in the socket 
                // staying open indefinitely (relatively speaking) so all we have to do is set it to a more realistic value:
                ServicePointManager.FindServicePoint(endpoint)
                    .ConnectionLeaseTimeout = (int)HttpClientConfig.ConnectionLifeTime.TotalMilliseconds;
                _endpoinsWithTcp[key] = true;
            }
        }
    }
}
#endif
