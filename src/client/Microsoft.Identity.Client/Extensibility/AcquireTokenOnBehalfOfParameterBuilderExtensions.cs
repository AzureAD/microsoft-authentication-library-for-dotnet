// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extension methods for the <see cref="AcquireTokenOnBehalfOfParameterBuilder" />
    /// </summary>
    public static class AcquireTokenOnBehalfOfParameterBuilderExtensions
    {
        /// <summary>
        /// Only affects <see cref="ILongRunningWebApi.InitiateLongRunningProcessInWebApi(IEnumerable{string}, string, ref string)"/>.
        /// When enabled, mimics MSAL 4.50.0 and below behavior - checks in cache for cached tokens first, 
        /// and if not found, then uses user assertion to request new tokens from AAD.
        /// When disabled (default behavior), doesn't search in cache, but uses the user assertion to retrieve tokens from AAD.
        /// </summary>
        /// <remarks>
        /// This method should only be used in specific cases for backwards compatibility. For most cases, rely on the default behavior
        /// of <see cref="ILongRunningWebApi.InitiateLongRunningProcessInWebApi(IEnumerable{string}, string, ref string)"/> and
        /// <see cref="ILongRunningWebApi.AcquireTokenInLongRunningProcess(IEnumerable{string}, string)"/> described in https://aka.ms/msal-net-long-running-obo .
        /// </remarks>
        /// <param name="builder"></param>
        /// <param name="searchInCache">Whether to search in cache.</param>
        /// <returns>The builder to chain the .With methods</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static AcquireTokenOnBehalfOfParameterBuilder WithSearchInCacheForLongRunningProcess(this AcquireTokenOnBehalfOfParameterBuilder builder, bool searchInCache = true)
        {
            builder.Parameters.SearchInCacheForLongRunningObo = searchInCache;
            return builder;
        }
    }
}
