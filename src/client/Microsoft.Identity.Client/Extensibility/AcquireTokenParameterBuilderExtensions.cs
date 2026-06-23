// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Advanced  
{
    /// <summary>
    /// </summary>
    public static class AcquireTokenParameterBuilderExtensions
    {
        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method has been moved to the Microsoft.Identity.Client.Extensibility namespace", false)]
        public static T WithExtraHttpHeaders<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder, 
            IDictionary<string, string> extraHttpHeaders)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            return Microsoft.Identity.Client.Extensibility.AcquireTokenParameterBuilderExtensions.WithExtraHttpHeaders(builder, extraHttpHeaders);
        }
    }
}

// Extensibility (new surface for WithExtraHttpHeaders)
namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensibility helpers for acquire token parameter builders.
    /// </summary>
    public static class AcquireTokenParameterBuilderExtensions
    {
        /// <summary>Adds additional HTTP headers to the token request.</summary>
        /// <param name="builder">Parameter builder for acquiring tokens.</param>
        /// <param name="extraHttpHeaders">Additional HTTP headers to add to the token request.</param>
        public static T WithExtraHttpHeaders<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            IDictionary<string, string> extraHttpHeaders)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return (T)builder;
        }

        /// <summary>
        /// Adds a key-value pair to the token cache key without sending it as a query parameter.
        /// Use this to partition cached access tokens (e.g., isolating short-lived sessions from regular
        /// sessions for the same user). Both <c>AcquireTokenByAuthorizationCode</c> and
        /// <c>AcquireTokenSilent</c> must use the same partition key to match cached entries.
        /// </summary>
        /// <param name="builder">The builder to chain .With methods.</param>
        /// <param name="key">The partition key name.</param>
        /// <param name="value">The partition key value.</param>
        /// <returns>The builder to chain .With methods.</returns>
        public static T WithCachePartitionKey<T>(
            this BaseAbstractAcquireTokenParameterBuilder<T> builder,
            string key,
            string value)
            where T : BaseAbstractAcquireTokenParameterBuilder<T>
        {
            return builder.WithCachePartitionKey(key, value, partitionRefreshToken: false);
        }

        /// <summary>
        /// Adds a key-value pair to the token cache key without sending it as a query parameter.
        /// Use this to partition cached tokens (e.g., isolating short-lived sessions from regular
        /// sessions for the same user). Both <c>AcquireTokenByAuthorizationCode</c> and
        /// <c>AcquireTokenSilent</c> must use the same partition key to match cached entries.
        /// </summary>
        /// <param name="builder">The builder to chain .With methods.</param>
        /// <param name="key">The partition key name.</param>
        /// <param name="value">The partition key value.</param>
        /// <param name="partitionRefreshToken">
        /// When <see langword="true"/>, the refresh token is also stored and looked up using
        /// the partition key. When <see langword="false"/>, only the access token is partitioned
        /// and the refresh token remains in the shared pool.
        /// </param>
        /// <returns>The builder to chain .With methods.</returns>
        public static T WithCachePartitionKey<T>(
            this BaseAbstractAcquireTokenParameterBuilder<T> builder,
            string key,
            string value,
            bool partitionRefreshToken)
            where T : BaseAbstractAcquireTokenParameterBuilder<T>
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty.", nameof(key));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            builder.CommonParameters.CacheKeyComponents ??= new SortedList<string, Func<CancellationToken, Task<string>>>();
            string capturedValue = value;
            builder.CommonParameters.CacheKeyComponents[key] = (CancellationToken _) => Task.FromResult(capturedValue);
            builder.CommonParameters.PartitionRefreshToken = partitionRefreshToken;
            return (T)builder;
        }

        /// <summary>
        /// Controls whether MSAL sends the reserved <c>offline_access</c> scope while continuing to
        /// send <c>openid</c> and <c>profile</c>. Only applicable to authorization code redemption flows.
        /// </summary>
        /// <param name="builder">The builder to chain .With methods.</param>
        /// <param name="offlineAccessScope">
        /// Set to <see langword="false"/> to omit <c>offline_access</c>. Set to <see langword="true"/>
        /// to preserve the default MSAL behavior of sending all reserved scopes.
        /// </param>
        /// <returns>The builder to chain .With methods.</returns>
        public static AcquireTokenByAuthorizationCodeParameterBuilder WithReservedScopes(
            this AcquireTokenByAuthorizationCodeParameterBuilder builder,
            bool offlineAccessScope)
        {
            builder.CommonParameters.SendOfflineAccessScope = offlineAccessScope;
            return builder;
        }
    }
}
