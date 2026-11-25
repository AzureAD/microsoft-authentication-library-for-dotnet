// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    /// <summary>
    /// An abstraction over objects that are able to listen to localhost url (e.g. http://localhost:1234)
    /// and to retrieve the authorization response via POST (form data).
    /// </summary>
    internal interface IUriInterceptor
    {
        /// <summary>
        /// Listens to http://localhost:{port} until an authorization response is accepted.
        /// Rejects requests without a form_post body and responses rejected by the response producer.
        /// Sends a display message or redirect for the accepted response.
        /// </summary>
        /// <remarks>Cancellation is very important as this is typically a long running unmonitored operation</remarks>
        /// <param name="port">the port to listen to</param>
        /// <param name="path">the path to listen in</param>
        /// <param name="responseProducer">Creates the browser response. A BadRequest response rejects the callback without ending the listener.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The accepted authorization response containing POST data.</returns>
        Task<AuthorizationResponse> ListenToSingleRequestAndRespondAsync(
            int port,
            string path,
            Func<AuthorizationResponse, MessageAndHttpCode> responseProducer,
            CancellationToken cancellationToken);
    }
}
