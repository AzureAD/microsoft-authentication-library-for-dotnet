// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
namespace Microsoft.Identity.Client
{
        /// <summary>
        /// Information about the client assertion that need to be generated See https://aka.ms/msal-net-client-assertion
        /// </summary>
        /// <remarks> Use the provided information to generate the client assertion payload </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public class AssertionRequestOptions {
        /// <summary>
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
        
        /// <summary>
        /// Client ID for which a signed assertion is requested
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// The intended token endpoint
        /// </summary>
        public string TokenEndpoint { get; set; }

        /// <summary>
        /// Claims to be included in the client assertion
        /// </summary>
        public string Claims { get; set; }

        /// <summary>
        /// Capabilities that the client application declares in the assertion.
        /// The service may require or interpret these capabilities in advanced scenarios.
        /// </summary>
        public IEnumerable<string> ClientCapabilities { get; set; }
    }
}
