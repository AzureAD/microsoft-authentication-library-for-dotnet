// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    public interface IMatsTelemetryBatch
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        IReadOnlyDictionary<string, bool> BoolValues { get; }

        /// <summary>
        /// 
        /// </summary>
        IReadOnlyDictionary<string, long> Int64Values { get; }

        /// <summary>
        /// 
        /// </summary>
        IReadOnlyDictionary<string, int> IntValues { get; }

        /// <summary>
        /// 
        /// </summary>
        IReadOnlyDictionary<string, string> StringValues { get; }
    }
}
