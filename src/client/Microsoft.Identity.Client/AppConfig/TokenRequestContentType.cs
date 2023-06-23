// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// The media content type for post requests made to the identity provider.
    /// </summary>
    public struct TokenRequestContentType
    {
        /// <summary>
        /// JSON media content type
        /// </summary>
        public const string JSON = "application/json";
    }
}
