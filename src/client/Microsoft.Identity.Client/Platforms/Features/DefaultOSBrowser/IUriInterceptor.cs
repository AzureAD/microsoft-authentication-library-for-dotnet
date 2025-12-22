// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    /// <summary>
    /// Result from intercepting an authorization response
    /// </summary>
    internal class AuthorizationResponse
    {
        public AuthorizationResponse(Uri requestUri, byte[] postData)
        {
            RequestUri = requestUri;
            PostData = postData;
        }

        public Uri RequestUri { get; set; }
        public byte[] PostData { get; set; }
        public bool IsFormPost => PostData != null && PostData.Length > 0;
    }

    /// <summary>
    /// An abstraction over objects that are able to listen to localhost url (e.g. http://localhost:1234)
    /// and to retrieve the authorization response via GET (query params) or POST (form data)
    /// </summary>
    internal interface IUriInterceptor
    {
        /// <summary>
        /// Listens to http://localhost:{port} and retrieve the authorization response.
        /// For GET requests, the response is in query params. For POST (form_post), the response is in the body.
        /// Then push back a response such as a display message or a redirect.
        /// </summary>
        /// <remarks>Cancellation is very important as this is typically a long running unmonitored operation</remarks>
        /// <param name="port">the port to listen to</param>
        /// <param name="path">the path to listen in</param>
        /// <param name="responseProducer">The message to be displayed, or url to be redirected to will be created by this callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authorization response containing either URI with query params or POST data</returns>
        Task<AuthorizationResponse> ListenToSingleRequestAndRespondAsync(
            int port,
            string path,
            Func<AuthorizationResponse, MessageAndHttpCode> responseProducer,
            CancellationToken cancellationToken);
    }
}
