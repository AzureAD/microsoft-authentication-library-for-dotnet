// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains metadata of the authentication result.
    /// </summary>
    public class AuthenticationResultMetadata
    {

        /// <summary>
        /// Constructor for the class AuthenticationResultMetadata
        /// <param name="tokenSource">The token source.</param>
        /// </summary>
        public AuthenticationResultMetadata(TokenSource tokenSource)
        {
            TokenSource = tokenSource;
        }

        /// <summary>
        /// The source of the token in the result.
        /// </summary>
        public TokenSource TokenSource { get; }

        /// <summary>
        /// Total time spent to service this request. Includes TimeInHttp and TimeInCache. All times in ms.
        /// </summary>
        public long TimeSpentInTotal { get; set; }

        /// <summary>
        /// Time spent in the cache to service the request
        /// </summary>
        public long TimeSpentInCache { get; set; }

        /// <summary>
        /// Time spent for HTTP communication
        /// </summary>
        public long TimeSpentInHttp { get; set; }
    }
}
