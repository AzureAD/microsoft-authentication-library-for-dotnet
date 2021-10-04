// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Parameters builder for the <see cref="IPublicClientApplication.AcquireTokenWithDeviceCode(IEnumerable{string}, Func{DeviceCodeResult, Task})"/>
    /// operation. See https://aka.ms/msal-net-device-code-flow
    /// </summary>
    public sealed class AcquireTokenWithDeviceCodeParameterBuilder :
        AbstractPublicClientAcquireTokenParameterBuilder<AcquireTokenWithDeviceCodeParameterBuilder>
    {
        private AcquireTokenWithDeviceCodeParameters Parameters { get; } = new AcquireTokenWithDeviceCodeParameters();

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenWithDeviceCode;

        /// <inheritdoc />
        internal AcquireTokenWithDeviceCodeParameterBuilder(IPublicClientApplicationExecutor publicClientApplicationExecutor)
            : base(publicClientApplicationExecutor)
        {
        }

        internal static AcquireTokenWithDeviceCodeParameterBuilder Create(
            IPublicClientApplicationExecutor publicClientApplicationExecutor,
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return new AcquireTokenWithDeviceCodeParameterBuilder(publicClientApplicationExecutor)
                   .WithScopes(scopes).WithDeviceCodeResultCallback(deviceCodeResultCallback);
        }

        /// <summary>
        /// Sets the Callback delegate so your application can
        /// interact with the user to direct them to authenticate (to a specific URL, with a code)
        /// </summary>
        /// <param name="deviceCodeResultCallback">callback containing information to show the user about how to authenticate
        /// and enter the device code.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenWithDeviceCodeParameterBuilder WithDeviceCodeResultCallback(
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            Parameters.DeviceCodeResultCallback = deviceCodeResultCallback;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return PublicClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByDeviceCode;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Validate()
        {
            base.Validate();

            if (Parameters.DeviceCodeResultCallback == null)
            {
                throw new ArgumentNullException(
                    nameof(Parameters.DeviceCodeResultCallback), 
                    "A deviceCodeResultCallback must be provided for Device Code authentication to work properly");
            }
        }
    }
}
