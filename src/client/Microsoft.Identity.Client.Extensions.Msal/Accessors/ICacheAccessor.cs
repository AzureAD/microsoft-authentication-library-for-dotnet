// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal
{
    internal interface ICacheAccessor
    {
        /// <summary>
        /// Deletes the cache
        /// </summary>
        void Clear();

        /// <summary>
        /// Reads the cache
        /// </summary>
        /// <returns>Unprotected cache</returns>
        byte[] Read();

        /// <summary>
        /// Writes the cache 
        /// </summary>
        /// <param name="data">Unprotected cache</param>
        void Write(byte[] data);

        /// <summary>
        /// Create an ICacheAccessor that can be used for validating persistence. This must
        /// be similar but not identical to the current accessor, so that to avoid overwriting an actual token cache
        /// </summary>
        ICacheAccessor CreateForPersistenceValidation();
    }
}
