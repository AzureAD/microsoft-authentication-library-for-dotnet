// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extensibility methods for <see cref="IPublicClientApplication"/>
    /// </summary>
    public static class PublicClientApplicationExtensions
    {
        /// <summary>
        /// Used to determine if the currently available broker is able to perform Proof-of-Possession.
        /// </summary>
        /// <returns>Boolean indicating if Proof-of-Possession is supported</returns>
        public static bool IsProofOfPossessionSupportedByClient(this IPublicClientApplication app)
        {
            if (app is PublicClientApplication pca)
            {
                return pca.IsProofOfPossessionSupportedByClient();
            }

            return false;
        }
    }
}
