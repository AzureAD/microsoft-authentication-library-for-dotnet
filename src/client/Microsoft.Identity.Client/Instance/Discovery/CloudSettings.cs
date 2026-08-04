// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Cloud-specific metadata for a single Azure cloud environment, modeled as an immutable
    /// string-keyed bag of values addressed by well-known key constants.
    /// </summary>
    /// <remarks>
    /// Values are addressed by well-known key constants (see <see cref="MsalCloudKeys"/>) rather than
    /// fixed typed properties, so new cloud-specific values can be added — and obsolete ones removed —
    /// without breaking callers: an unknown/removed key simply misses, and the consumer falls back to a
    /// documented default. Typed convenience accessors are provided as extension methods
    /// (see <see cref="CloudSettingsExtensions"/>).
    /// </remarks>
    public sealed class CloudSettings
    {
        private static readonly IReadOnlyDictionary<string, string> s_empty =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0, StringComparer.OrdinalIgnoreCase));

        private readonly IReadOnlyDictionary<string, string> _values;

        /// <summary>
        /// Initializes a new <see cref="CloudSettings"/> instance.
        /// </summary>
        /// <param name="values">
        /// The cloud-specific values, keyed by the constants in <see cref="MsalCloudKeys"/> (and, for
        /// higher-level SDKs, their own key namespaces). May be <c>null</c> (treated as empty).
        /// Lookup is case-insensitive.
        /// </param>
        public CloudSettings(IReadOnlyDictionary<string, string> values)
        {
            if (values is null)
            {
                _values = s_empty;
            }
            else
            {
                // Copy into a case-insensitive dictionary and wrap it read-only so the instance is
                // immutable and callers cannot downcast Values to mutate a shared (e.g. singleton) bag.
                var copy = new Dictionary<string, string>(values.Count, StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    copy[kvp.Key] = kvp.Value;
                }

                _values = new ReadOnlyDictionary<string, string>(copy);
            }
        }

        /// <summary>
        /// The raw, read-only bag of cloud-specific values for this cloud, keyed by the constants in
        /// <see cref="MsalCloudKeys"/> (and higher-level SDK key namespaces).
        /// </summary>
        public IReadOnlyDictionary<string, string> Values => _values;

        /// <summary>
        /// Attempts to get a cloud-specific value by key.
        /// </summary>
        /// <param name="key">A key constant, e.g. from <see cref="MsalCloudKeys"/>.</param>
        /// <param name="value">The value if present; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the key is present; otherwise <c>false</c>.</returns>
        public bool TryGetValue(string key, out string value)
        {
            if (key is not null && _values.TryGetValue(key, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Gets a cloud-specific value by key, or <c>null</c> if the key is not present.
        /// </summary>
        /// <param name="key">A key constant, e.g. from <see cref="MsalCloudKeys"/>.</param>
        /// <returns>The value, or <c>null</c>.</returns>
        public string GetValueOrDefault(string key)
        {
            return TryGetValue(key, out string value) ? value : null;
        }

        /// <summary>
        /// Merges two cloud settings for the same host into a new instance, with <paramref name="higher"/>
        /// taking precedence per key and <paramref name="lower"/> supplying any keys the higher layer does
        /// not set. Either argument may be <c>null</c>.
        /// </summary>
        internal static CloudSettings Merge(CloudSettings lower, CloudSettings higher)
        {
            if (higher is null)
            {
                return lower;
            }

            if (lower is null)
            {
                return higher;
            }

            var mergedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kvp in lower.Values)
            {
                mergedValues[kvp.Key] = kvp.Value;
            }

            foreach (KeyValuePair<string, string> kvp in higher.Values)
            {
                mergedValues[kvp.Key] = kvp.Value;
            }

            return new CloudSettings(mergedValues);
        }
    }
}
