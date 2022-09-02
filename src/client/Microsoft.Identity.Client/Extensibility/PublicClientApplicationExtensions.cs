// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extensibility methods for <see cref="IPublicClientApplication"/>
    /// </summary>
    public static class PublicClientApplicationExtensions
    {
        /// <summary>
        /// Used to determine if the currently available broker is able to perform Proof-of-Possesion.
        /// </summary>
        /// <returns>Boolean indicating Proof-of-Possesion is supported</returns>
        public static bool IsProofOfPosessionSupportedByClient(this IPublicClientApplication app)
        {
            if (app is PublicClientApplication pca)
            {
                return pca.IsProofOfPosessionSupportedByClient();
            }

            return false;
        }
    }
}
