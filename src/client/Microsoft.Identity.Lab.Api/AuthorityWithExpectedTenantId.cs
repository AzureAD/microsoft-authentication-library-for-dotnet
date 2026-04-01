// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Lab.Api
{
    /// <summary>
    /// Represents an authority URI and its expected tenant ID for testing purposes.
    /// </summary>
    public class AuthorityWithExpectedTenantId
    {
        /// <summary>
        /// Gets or sets the authority URI.
        /// </summary>
        public Uri Authority { get; set; }

        /// <summary>
        /// Gets or sets the expected tenant ID.
        /// </summary>
        public string ExpectedTenantId { get; set; }

        /// <summary>
        /// Converts the authority and expected tenant ID to an object array.
        /// </summary>
        /// <returns>An array containing the authority and expected tenant ID.</returns>
        public object[] ToObjectArray()
        {
            return new object[] { Authority, ExpectedTenantId };
        }
    }
}
