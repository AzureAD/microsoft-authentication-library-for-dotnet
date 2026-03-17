// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Common
{
    /// <summary>
    /// RecordingHandler is a custom HTTP message handler that intercepts HTTP requests and responses, allowing for recording or logging of the traffic. It takes an action delegate as a parameter, which is invoked with the HTTP request and response whenever a request is sent through this handler. This can be useful in testing scenarios where you want to capture and assert on the HTTP interactions made by the code under test. The SendAsync method is overridden to perform the recording action after the base handler processes the request and obtains the response.
    /// </summary>
    public class RecordingHandler : DelegatingHandler
    {
        private readonly Action<HttpRequestMessage, HttpResponseMessage> _recordingAction;

        /// <summary>
        /// Constructs a new instance of the RecordingHandler class with the specified recording action. The recording action is a delegate that will be called with the HTTP request and response whenever a request is sent through this handler. This allows for custom logic to be executed, such as logging or storing the requests and responses for later analysis in tests.
        /// </summary>
        /// <param name="recordingAction"></param>
        public RecordingHandler(Action<HttpRequestMessage, HttpResponseMessage> recordingAction)
        {
            _recordingAction = recordingAction;
        }

        /// <summary>
        /// Sends an HTTP request asynchronously and records the request and response using the provided recording action. This method overrides the SendAsync method of the DelegatingHandler class, allowing it to intercept the HTTP traffic. After calling the base SendAsync method to get the response, it invokes the recording action with both the request and response, enabling custom processing such as logging or storing the interactions for testing purposes. Finally, it returns the HTTP response to the caller.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            _recordingAction.Invoke(request, response);
            return response;
        }
    }
}
