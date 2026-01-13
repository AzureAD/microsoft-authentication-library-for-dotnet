// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Parameter builder for the <see cref="IByUsernameAndPassword.AcquireTokenByUsernamePassword(IEnumerable{string}, string, string)"/>
    /// operation. See https://aka.ms/msal-net-up
    /// </summary>
    public sealed class AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder>
    {
        private AcquireTokenByUsernamePasswordParameters Parameters { get; } = new AcquireTokenByUsernamePasswordParameters();

        internal AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            string username,
            string password)
            : base(confidentialClientApplicationExecutor)
        {
            Parameters.Username = username;
            Parameters.Password = password;
        }

        internal static AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes,
            string username,
            string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            return new AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder(confidentialClientApplicationExecutor, username, password)
                   .WithScopes(scopes);
        }

        /// <summary>
        /// Applicable to first-party applications only, this method also allows to specify 
        /// if the <see href="https://datatracker.ietf.org/doc/html/rfc7517#section-4.7">x5c claim</see> should be sent to Azure AD.
        /// Sending the x5c enables application developers to achieve easy certificate roll-over in Azure AD:
        /// this method will send the certificate chain to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni
        /// </summary>
        /// <param name="withSendX5C"><c>true</c> if the x5c should be sent. Otherwise <c>false</c>.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <inheritdoc/>
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc/>
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByUsernamePassword;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (Parameters.SendX5C == null)
            {
                Parameters.SendX5C = ServiceBundle.Config?.SendX5C ?? false;
            }
        }
    }
}
