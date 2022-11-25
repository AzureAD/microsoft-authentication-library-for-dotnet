// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for parameter builders common to public client application and confidential
    /// client application token acquisition operations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractClientAppBaseAcquireTokenParameterBuilder<T> : AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AbstractClientAppBaseAcquireTokenParameterBuilder(IClientApplicationBaseExecutor clientApplicationBaseExecutor)
            : base(clientApplicationBaseExecutor.ServiceBundle)
        {
            ClientApplicationBaseExecutor = clientApplicationBaseExecutor;
        }

        internal IClientApplicationBaseExecutor ClientApplicationBaseExecutor { get; }

        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
        }
    }
}
