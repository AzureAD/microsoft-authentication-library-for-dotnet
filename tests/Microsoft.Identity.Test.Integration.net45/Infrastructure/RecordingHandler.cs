// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.net45.Infrastructure
{
    public class RecordingHandler : DelegatingHandler
    {
        private readonly Action<HttpRequestMessage, HttpResponseMessage> _recordingAction;

        public RecordingHandler(Action<HttpRequestMessage, HttpResponseMessage> recordingAction)
        {
            _recordingAction = recordingAction;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            _recordingAction.Invoke(request, response);
            return response;
        }
    }
}
