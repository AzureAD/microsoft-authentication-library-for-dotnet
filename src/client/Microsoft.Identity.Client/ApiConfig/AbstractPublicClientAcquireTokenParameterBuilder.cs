// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for public client application token request builders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractPublicClientAcquireTokenParameterBuilder<T>
        : AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AbstractPublicClientAcquireTokenParameterBuilder(IPublicClientApplicationExecutor publicClientApplicationExecutor)
        {
            PublicClientApplicationExecutor = publicClientApplicationExecutor;
        }

        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
        }

        /// <summary>
        /// </summary>
        internal IPublicClientApplicationExecutor PublicClientApplicationExecutor { get; }
    }
}
