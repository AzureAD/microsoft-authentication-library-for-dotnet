// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class CredentialManagedIdentitySource : AbstractManagedIdentity
    {
        /// <summary>
        /// Factory method to create an instance of `CredentialManagedIdentitySource`.
        /// </summary>
        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            requestContext.Logger.Info(() => "[Managed Identity] Using credential based managed identity.");

            return new CredentialManagedIdentitySource(requestContext);
        }

        private CredentialManagedIdentitySource(RequestContext requestContext) :
            base(requestContext, ManagedIdentitySource.Credential)
        {
        }

        /// <summary>
        /// Even though the Credential flow does not use this request, we need to satisfy the abstract contract.
        /// Return a minimal, valid ManagedIdentityRequest using the fixed credential endpoint.
        /// </summary>
        /// <param name="resource">The resource identifier (ignored in this flow).</param>
        /// <returns>A ManagedIdentityRequest instance using the credential endpoint.</returns>
        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            // Return a minimal request with the fixed credential endpoint.
            return new ManagedIdentityRequest(
                HttpMethod.Post,
                new Uri("http://169.254.169.254/metadata/identity/credential?cred-api-version=1.0"));
        }
    }
}
