// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// A ready-made, mutable <see cref="ICloudConfiguration"/> that lets a caller register cloud-specific
    /// metadata for one or more authority hosts at runtime, optionally layered over a fallback provider
    /// (typically <see cref="KnownCloudConfiguration.Default"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This lets a caller add a cloud MSAL doesn't ship, or adjust one it does, without hand-implementing
    /// <see cref="ICloudConfiguration"/>. Register a whole cloud with
    /// <see cref="AddOrUpdate(string, System.Collections.Generic.IReadOnlyDictionary{string, string})"/>
    /// or a single value with <see cref="AddOrUpdate(string, string, string)"/>, then read values back with
    /// <see cref="GetSettingsByAuthorityHost(string)"/>.
    /// </para>
    /// <para>
    /// Resolution layers the registered values over the optional <c>fallback</c> <b>per key</b>: a key
    /// registered here wins, a key not registered here falls back to the same host in <c>fallback</c>, and
    /// a host unknown to both resolves to <c>null</c>. Values are keyed by <see cref="MsalCloudKeys"/>
    /// (and, for higher-level SDKs, their own key namespaces).
    /// </para>
    /// </remarks>
    public sealed class InMemoryCloudConfiguration : ICloudConfiguration
    {
        private readonly ICloudConfiguration _fallback;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _overridesByHost =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates an empty in-memory cloud configuration.
        /// </summary>
        /// <param name="fallback">
        /// Optional lower-precedence provider consulted, per key, for values not registered here — commonly
        /// <see cref="KnownCloudConfiguration.Default"/> so MSAL's built-in clouds remain resolvable. When
        /// <c>null</c>, only hosts registered here resolve (a complete override of the built-in defaults).
        /// </param>
        public InMemoryCloudConfiguration(ICloudConfiguration fallback = null)
        {
            _fallback = fallback;
        }

        /// <summary>
        /// Registers or updates cloud-specific values for an authority host, merged <b>per key</b> over any
        /// values already registered for that host (and, at resolution time, over the fallback). Keys in
        /// <paramref name="values"/> win; keys not supplied are left untouched.
        /// </summary>
        /// <param name="authorityHost">
        /// The authority host these values apply to (e.g., "login.mynewcloud.example"). Required.
        /// </param>
        /// <param name="values">
        /// The cloud-specific values, keyed by <see cref="MsalCloudKeys"/> constants (e.g.,
        /// <see cref="MsalCloudKeys.TokenExchangeAudience"/>). Required.
        /// </param>
        /// <returns>This instance, to allow chaining.</returns>
        public InMemoryCloudConfiguration AddOrUpdate(string authorityHost, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(authorityHost))
            {
                throw new ArgumentNullException(nameof(authorityHost));
            }

            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            ConcurrentDictionary<string, string> bag = GetOrAddBag(authorityHost);
            foreach (KeyValuePair<string, string> kvp in values)
            {
                bag[kvp.Key] = kvp.Value;
            }

            return this;
        }

        /// <summary>
        /// Registers or updates a <b>single</b> cloud-specific value for an authority host, merged per key
        /// over any values already registered for that host (and, at resolution time, over the fallback).
        /// Use this to adjust or add one value for a cloud while leaving its other values as-is.
        /// </summary>
        /// <param name="authorityHost">
        /// The authority host this value applies to (e.g., "login.microsoftonline.us"). Required.
        /// </param>
        /// <param name="key">The value's key, e.g. <see cref="MsalCloudKeys.TokenExchangeAudience"/>. Required.</param>
        /// <param name="value">The value to set. Required.</param>
        /// <returns>This instance, to allow chaining.</returns>
        public InMemoryCloudConfiguration AddOrUpdate(string authorityHost, string key, string value)
        {
            if (string.IsNullOrEmpty(authorityHost))
            {
                throw new ArgumentNullException(nameof(authorityHost));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            GetOrAddBag(authorityHost)[key] = value;
            return this;
        }

        /// <inheritdoc/>
        public CloudSettings GetSettingsByAuthorityHost(string authorityHost)
        {
            if (string.IsNullOrEmpty(authorityHost))
            {
                return null;
            }

            CloudSettings fallbackSettings = _fallback?.GetSettingsByAuthorityHost(authorityHost);

            if (!_overridesByHost.TryGetValue(authorityHost, out ConcurrentDictionary<string, string> bag))
            {
                return fallbackSettings;
            }

            var overrides = new CloudSettings(bag);
            return CloudSettings.Merge(fallbackSettings, overrides);
        }

        private ConcurrentDictionary<string, string> GetOrAddBag(string authorityHost)
        {
            return _overridesByHost.GetOrAdd(
                authorityHost,
                _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }
    }
}
