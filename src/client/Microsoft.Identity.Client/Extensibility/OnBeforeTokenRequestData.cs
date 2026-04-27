// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Authentication request details
    /// </summary>
    /// <remarks>
    /// Constructor. 
    /// </remarks>
    /// <remarks>Apps should not have to use this constructor. It is provided for testability purposes.</remarks>
    public sealed class OnBeforeTokenRequestData(
        IDictionary<string, string> bodyParameters,
        IDictionary<string, string> headers,
        Uri requestUri,
        CancellationToken cancellationToken)
    {

        /// <summary>
        /// Parameters which will be sent in the request body, as POST parameters.
        /// </summary>
        public IDictionary<string, string> BodyParameters { get; } = bodyParameters;

        /// <summary>
        /// Headers which will be sent with the request.
        /// </summary>
        public IDictionary<string, string> Headers { get; } = headers;

        /// <summary>
        /// The token endpoint, including any query parameters, where the request is being sent to.
        /// </summary>
        public Uri RequestUri { get; set; } = requestUri;

        /// <summary>
        /// The cancellation token associated with the request
        /// </summary>
        public CancellationToken CancellationToken { get; } = cancellationToken;
    }
}
