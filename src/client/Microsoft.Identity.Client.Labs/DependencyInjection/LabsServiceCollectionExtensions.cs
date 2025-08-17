// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Labs.Internal;

namespace Microsoft.Identity.Client.Labs
{
    /// <summary>
    /// Extension methods for registering Labs services in a dependency injection container.
    /// </summary>
    public static class LabsServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Labs resolvers and configuration with the provided service collection.
        /// </summary>
        /// <param name="services">The target service collection.</param>
        /// <param name="configure">A delegate used to configure <see cref="LabsOptions"/>.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="configure"/> is <c>null</c>.</exception>
        public static IServiceCollection AddLabsIdentity(
            this IServiceCollection services,
            Action<LabsOptions> configure)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (configure is null)
                throw new ArgumentNullException(nameof(configure));

            services.Configure(configure);

            services.AddSingleton<AccountMapAggregator>();
            services.AddSingleton<AppMapAggregator>();

            services.AddSingleton<ISecretStore>(sp =>
            {
                var opt = sp.GetRequiredService<IOptions<LabsOptions>>().Value;
                if (opt.KeyVaultUri is null)
                {
                    throw new InvalidOperationException("LabsOptions.KeyVaultUri must be set.");
                }

                return new KeyVaultSecretStore(opt.KeyVaultUri);
            });

            services.AddSingleton<IAccountResolver, AccountResolver>();
            services.AddSingleton<IAppResolver, AppResolver>();

            return services;
        }

        /// <summary>
        /// Registers an SDK-specific account map provider with the service collection.
        /// </summary>
        /// <typeparam name="T">The type that implements <see cref="IAccountMapProvider"/>.</typeparam>
        /// <param name="services">The target service collection.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddAccountMapProvider<T>(this IServiceCollection services)
            where T : class, IAccountMapProvider
            => services.AddSingleton<IAccountMapProvider, T>();

        /// <summary>
        /// Registers an SDK-specific app map provider with the service collection.
        /// </summary>
        /// <typeparam name="T">The type that implements <see cref="IAppMapProvider"/>.</typeparam>
        /// <param name="services">The target service collection.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddAppMapProvider<T>(this IServiceCollection services)
            where T : class, IAppMapProvider
            => services.AddSingleton<IAppMapProvider, T>();
    }
}
