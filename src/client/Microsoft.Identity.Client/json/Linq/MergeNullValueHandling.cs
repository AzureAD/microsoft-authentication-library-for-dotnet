using System;

namespace Microsoft.Identity.Json.Linq
{
    /// <summary>
    /// Specifies how null value properties are merged.
    /// </summary>
    [Flags]
    internal enum MergeNullValueHandling
    {
        /// <summary>
        /// The content's null value properties will be ignored during merging.
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// The content's null value properties will be merged.
        /// </summary>
        Merge = 1
    }
}
