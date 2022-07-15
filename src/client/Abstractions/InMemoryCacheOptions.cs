// Copyright (c) Microsoft Corporation. All rights reserved.    
// Licensed under the MIT License.


using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.ServiceEssentials
{
    /// <summary>
    /// </summary>
    public class InMemoryCacheOptions : IOptions<InMemoryCacheOptions>
    {
        /// <summary>
        /// category settings
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, MemoryCacheOptions> CategoryOptions { get; set; } = new Dictionary<string, MemoryCacheOptions>();
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// 
        /// </summary>
        InMemoryCacheOptions IOptions<InMemoryCacheOptions>.Value => this;
    }
}
