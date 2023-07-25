// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for builders of token requests, which attempt to acquire a token
    /// based on the provided parameters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseAbstractAcquireTokenParameterBuilder<T>
        where T : BaseAbstractAcquireTokenParameterBuilder<T>
    {

        internal IServiceBundle ServiceBundle { get; }

        /// <summary>
        /// Default constructor for AbstractAcquireTokenParameterBuilder.
        /// </summary>
        protected BaseAbstractAcquireTokenParameterBuilder() { }

        internal BaseAbstractAcquireTokenParameterBuilder(IServiceBundle serviceBundle)
        {
            ServiceBundle = serviceBundle;
        }

        internal AcquireTokenCommonParameters CommonParameters { get; } = new AcquireTokenCommonParameters();

        /// <summary>
        /// Executes the Token request asynchronously, with a possibility of cancelling the
        /// asynchronous method.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token. See <see cref="CancellationToken"/> </param>
        /// <returns>Authentication result containing a token for the requested scopes and parameters
        /// set in the builder.</returns>
        /// <remarks>
        /// <para>
        /// Cancellation is not guaranteed, it is best effort. If the operation reaches a point of no return, e.g.
        /// tokens are acquired and written to the cache, the task will complete even if cancellation was requested.
        /// Do not rely on cancellation tokens for strong consistency.
        /// </para>
        /// <para>
        /// To learn more about potential exceptions thrown by the function, refer to <see href="https://aka.ms/msal-net-exceptions">Exceptions in MSAL.NET</see>.
        /// </para>
        /// </remarks>
        public abstract Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);

        internal abstract ApiEvent.ApiIds CalculateApiEventId();

        /// <summary>
        /// Executes the Token request asynchronously.
        /// </summary>
        /// <returns>Authentication result containing a token for the requested scopes and parameters
        /// set in the builder.</returns>
        public Task<AuthenticationResult> ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Sets the correlation id to be used in the authentication request. Used to track a request in the logs of both the SDK and the Identity Provider service.
        /// If not set, a random one will be generated. 
        /// </summary>
        /// <param name="correlationId">Correlation id of the authentication request.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        public T WithCorrelationId(Guid correlationId)
        {
            CommonParameters.UserProvidedCorrelationId = correlationId;
            CommonParameters.UseCorrelationIdFromUser = true;
            return (T)this;
        }

        /// <summary>
        /// Validates the parameters of the AcquireToken operation.
        /// </summary>
        protected virtual void Validate()
        {
        }

        internal void ValidateAndCalculateApiId()
        {
            Validate();
            CommonParameters.ApiId = CalculateApiEventId();
            CommonParameters.CorrelationId = CommonParameters.UseCorrelationIdFromUser ? CommonParameters.UserProvidedCorrelationId : Guid.NewGuid();
        }

        internal void ValidateUseOfExperimentalFeature([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (!ServiceBundle.Config.ExperimentalFeaturesEnabled)
            {
                throw new MsalClientException(
                    MsalError.ExperimentalFeature,
                    MsalErrorMessage.ExperimentalFeature(memberName));
            }
        }
    }
}
