// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Extensibility
{

    /// <summary>
    /// Extensions for all AcquireToken methods
    /// </summary>
    public static class AbstractConfidentialClientAcquireTokenParameterBuilderExtension
    {
        /// <summary>
        /// Intervenes in the request pipeline, by executing a user provided delegate before MSAL makes the token request. 
        /// The delegate can modify the request payload by adding or removing  body parameters and headers. <see cref="OnBeforeTokenRequestData"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="onBeforeTokenRequestHandler">An async delegate which gets invoked just before MSAL makes a token request</param>
        /// <returns>The builder to chain other options to.</returns>
        public static AbstractAcquireTokenParameterBuilder<T> OnBeforeTokenRequest<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder, 
            Func<OnBeforeTokenRequestData, Task> onBeforeTokenRequestHandler) 
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (builder.CommonParameters.OnBeforeTokenRequestHandler == null)
            {
                builder.CommonParameters.OnBeforeTokenRequestHandler = new List<Func<OnBeforeTokenRequestData, Task>> { onBeforeTokenRequestHandler };
            }
            else
            {
                builder.CommonParameters.OnBeforeTokenRequestHandler.Add(onBeforeTokenRequestHandler);
            }

            return builder;
        }

        /// <summary>
        /// Binds the token to a key in the cache.No cryptographic operations is performed on the token.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="keyId">A key id to which the access token is associated. The token will not be retrieved from the cache unless the same key id is presented. Can be null.</param>
        /// <param name="expectedTokenTypeFromAad">AAD issues several types of bound tokens. MSAL checks the token type, which needs to match the value set by ESTS. Normal POP tokens have this as "pop"</param>
        /// <returns>the builder</returns>
        public static AbstractAcquireTokenParameterBuilder<T> WithProofOfPosessionKeyId<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            string keyId,
            string expectedTokenTypeFromAad = "Bearer")
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId));
            }

            builder.ValidateUseOfExperimentalFeature();
            builder.CommonParameters.AuthenticationOperation = new ExternalBoundTokenScheme(keyId, expectedTokenTypeFromAad);

            return builder;
        }

        /// <summary>
        /// Enables client applications to provide a custom authentication operation to be used in the token acquisition request.
        /// </summary>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="authenticationExtension">The implementation of the authentication operation.</param>
        /// <returns></returns>
        public static AbstractAcquireTokenParameterBuilder<T> WithAuthenticationExtension<T>(
           this AbstractAcquireTokenParameterBuilder<T> builder,
           MsalAuthenticationExtension authenticationExtension)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (authenticationExtension.OnBeforeTokenRequestHandler != null)
            {
                if (builder.CommonParameters.OnBeforeTokenRequestHandler == null)
                {
                    builder.CommonParameters.OnBeforeTokenRequestHandler = new List<Func<OnBeforeTokenRequestData, Task>> { authenticationExtension.OnBeforeTokenRequestHandler };
                }
                else
                {
                    builder.CommonParameters.OnBeforeTokenRequestHandler.Add(authenticationExtension.OnBeforeTokenRequestHandler);
                }
            }

            if (authenticationExtension.AuthenticationOperation != null)
                builder.WithAuthenticationOperation(authenticationExtension.AuthenticationOperation);

            if (authenticationExtension.AdditionalCacheParameters != null)
                builder.WithAdditionalCacheParameters(authenticationExtension.AdditionalCacheParameters);

            return builder;
        }
        
        /// <summary>
        /// Specifies additional parameters acquired from authentication responses to be cached with the access token that are normally not included in the cache object.
        /// these values can be read from the <see cref="AuthenticationResult.AdditionalResponseParameters"/> parameter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="cacheParameters">Additional parameters to cache</param>
        /// <returns></returns>
        public static AbstractAcquireTokenParameterBuilder<T> WithAdditionalCacheParameters<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder, 
            IEnumerable<string> cacheParameters)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (cacheParameters != null && !cacheParameters.Any())
            {
                return builder;
            }

            builder.ValidateUseOfExperimentalFeature();

            //Check if the cache parameters are already initialized, if so, add to the existing list
            if (builder.CommonParameters.AdditionalCacheParameters != null)
            {
                builder.CommonParameters.AdditionalCacheParameters.AddRange(cacheParameters);
            }
            else
            {
                builder.CommonParameters.AdditionalCacheParameters = cacheParameters.ToList<string>();
            }
            return builder;
        }

        /// <summary>
        /// Specifies additional cache key components to use when caching and retrieving tokens.
        /// </summary>
        /// <param name="cacheKeyComponents">The list of additional cache key components.</param>
        /// <param name="builder"></param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>This api can be used to associate certificate key identifiers along with other keys with a particular token.</description></item>
        /// <item><description>In order for the tokens to be successfully retrieved from the cache, all components used to cache the token must be provided.</description></item>
        /// </list>
        /// </remarks>
        internal static AbstractAcquireTokenParameterBuilder<T> WithAdditionalCacheKeyComponents<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            IDictionary<string, Func<CancellationToken, Task<string>>> cacheKeyComponents)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (cacheKeyComponents == null || cacheKeyComponents.Count == 0)
            {
                //no-op
                return builder;
            }

            if (builder.CommonParameters.CacheKeyComponents == null)
            {
                builder.CommonParameters.CacheKeyComponents = new SortedList<string, Func<CancellationToken, Task<string>>>(cacheKeyComponents);
            }
            else
            {
                foreach (var kvp in cacheKeyComponents)
                {
                    // Key conflicts are not allowed, it is expected for this method to fail.
                    builder.CommonParameters.CacheKeyComponents.Add(kvp.Key, kvp.Value);
                }
            }

            return builder;
        }

        // Add this new overload alongside the existing WithAdditionalCacheKeyComponents method

        /// <summary>
        /// Specifies additional cache key components to use when caching and retrieving tokens (synchronous version).
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="cacheKeyComponents">The dictionary of additional cache key components with synchronous string values.</param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>This api can be used to associate custom parameters along with other keys with a particular token.</description></item>
        /// <item><description>In order for the tokens to be successfully retrieved from the cache, all components used to cache the token must be provided.</description></item>
        /// <item><description>This synchronous overload avoids async wrapper allocation by converting string values directly to cache key components.</description></item>
        /// </list>
        /// </remarks>
        internal static AbstractAcquireTokenParameterBuilder<T> WithAdditionalCacheKeyComponents<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            IDictionary<string, string> cacheKeyComponents)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (cacheKeyComponents == null || cacheKeyComponents.Count == 0)
            {
                //no-op
                return builder;
            }

            // Convert sync string values to async Func<CancellationToken, Task<string>>
            // This conversion happens once at configuration time, not on the hot path
            var asyncComponents = new SortedList<string, Func<CancellationToken, Task<string>>>();
            foreach (var kvp in cacheKeyComponents)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                {
                    continue; // Skip null/whitespace keys or values
                }

                // Create async wrapper - this allocation happens once at config time
                var value = kvp.Value;
                asyncComponents.Add(kvp.Key, (_) => Task.FromResult(value));
            }

            if (asyncComponents.Count == 0)
            {
                return builder;
            }

            if (builder.CommonParameters.CacheKeyComponents == null)
            {
                builder.CommonParameters.CacheKeyComponents = asyncComponents;
            }
            else
            {
                foreach (var kvp in asyncComponents)
                {
                    // Key conflicts are not allowed, it is expected for this method to fail.
                    builder.CommonParameters.CacheKeyComponents.Add(kvp.Key, kvp.Value);
                }
            }

            return builder;
        }

        /// <summary>
        /// Specifies an FMI path to be used for the client assertion. This lets higher level APIs like Id.Web 
        /// provide credentials which are FMI sensitive.
        /// Important: tokens are associated with the credential FMI path, which impacts cache lookups
        /// This is an extensibility API and should not be used by applications.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="fmiPath">The FMI path to use for client assertion.</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="ArgumentNullException">Thrown when fmiPath is null or whitespace.</exception>
        public static AbstractAcquireTokenParameterBuilder<T> WithFmiPathForClientAssertion<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            string fmiPath)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.ValidateUseOfExperimentalFeature();

            if (string.IsNullOrWhiteSpace(fmiPath))
            {
                throw new ArgumentNullException(nameof(fmiPath));
            }

            builder.CommonParameters.ClientAssertionFmiPath = fmiPath;

            // Add the fmi_path to the cache key so that it is used for cache lookups
            var cacheKey = new SortedList<string, Func<CancellationToken, Task<string>>>
            {
                { "credential_fmi_path", (CancellationToken ct) => Task.FromResult(fmiPath) }
            };

            WithAdditionalCacheKeyComponents(builder, cacheKey);

            return builder;
        }

        /// <summary>
        /// Specifies extra claims to be included in the client assertion. 
        /// These claims will be merged with default claims when the client assertion is generated.
        /// This lets higher level APIs like Microsoft.Identity.Web provide additional claims for the client assertion.
        /// Important: tokens are associated with the extra client assertion claims, which impacts cache lookups.
        /// This is an extensibility API and should not be used by applications directly.
        /// </summary>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="clientAssertionClaims">Additional claims in JSON format to be signed in the client assertion.</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="ArgumentNullException">Thrown when clientAssertionClaims is null or whitespace.</exception>
        public static AbstractAcquireTokenParameterBuilder<T> WithExtraClientAssertionClaims<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            string clientAssertionClaims)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (string.IsNullOrWhiteSpace(clientAssertionClaims))
            {
                throw new ArgumentNullException(nameof(clientAssertionClaims));
            }

            builder.CommonParameters.ExtraClientAssertionClaims = clientAssertionClaims;

            // Add the extra claims to the cache key so different claims result in different cache entries
            var cacheKey = new SortedList<string, Func<CancellationToken, Task<string>>>
            {
                { "extra_client_assertion_claims", (CancellationToken ct) => Task.FromResult(clientAssertionClaims) }
            };

            WithAdditionalCacheKeyComponents(builder, cacheKey);

            return builder;
        }
    }   
}
