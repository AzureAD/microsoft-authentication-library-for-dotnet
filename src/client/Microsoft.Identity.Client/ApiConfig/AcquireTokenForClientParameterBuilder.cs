// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for AcquireTokenForClient (used in client credential flows, in daemon applications).
    /// See https://aka.ms/msal-net-client-credentials
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed class AcquireTokenForClientParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenForClientParameterBuilder>
    {
        internal AcquireTokenForClientParameters Parameters { get; } = new AcquireTokenForClientParameters();

        /// <inheritdoc/>
        internal AcquireTokenForClientParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor)
            : base(confidentialClientApplicationExecutor)
        {
        }

        internal static AcquireTokenForClientParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes)
        {
            var builder = new AcquireTokenForClientParameterBuilder(confidentialClientApplicationExecutor).WithScopes(scopes);

            if (!string.IsNullOrEmpty(confidentialClientApplicationExecutor.ServiceBundle.Config.CertificateIdToAssociateWithToken))
            {
                builder.WithAdditionalCacheKeyComponents(new SortedList<string, Func<CancellationToken, Task<string>>>
                {
                    { Constants.CertSerialNumber, (CancellationToken ct) => { return Task.FromResult(confidentialClientApplicationExecutor.ServiceBundle.Config.CertificateIdToAssociateWithToken); } }
                });
            }

            return builder;
        }

        /// <summary>
        /// Specifies if the client application should ignore access tokens when reading the token cache. 
        /// New tokens will still be written to the application token cache.
        /// By default the token is taken from the application token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">
        /// If <c>true</c>, the request will ignore cached access tokens on read, but will still write them to the cache once obtained from the identity provider. The default is <c>false</c>
        /// </param>
        /// <remarks>
        /// Do not use this flag except in well understood cases. Identity providers will throttle clients that make too many similar token requests.
        /// </remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForClientParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
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
        public AcquireTokenForClientParameterBuilder WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <summary>
        /// Specifies that the certificate provided will be used for PoP tokens with mTLS (Mutual TLS) authentication.
        /// For more information, refer to the <see href="https://aka.ms/mtls-pop">Proof-of-Possession documentation</see>.
        /// </summary>
        /// <returns>The current instance of <see cref="AcquireTokenForClientParameterBuilder"/> to enable method chaining.</returns>
        public AcquireTokenForClientParameterBuilder WithMtlsProofOfPossession()
        {
            if (ServiceBundle.Config.ClientCredential is CertificateClientCredential certificateCredential)
            {
                if (certificateCredential.Certificate == null)
                {
                    throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                CommonParameters.AuthenticationOperation = new MtlsPopAuthenticationOperation(certificateCredential.Certificate);
                CommonParameters.MtlsCertificate = certificateCredential.Certificate;               
            }

            CommonParameters.IsMtlsPopRequested = true;
            return this;
        }

        /// <summary>
        /// Please use WithAzureRegion on the ConfidentialClientApplicationBuilder object
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use WithAzureRegion on the ConfidentialClientApplicationBuilder object", true)]
        public AcquireTokenForClientParameterBuilder WithAzureRegion(bool useAzureRegion)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Please use WithAzureRegion on the ConfidentialClientApplicationBuilder object
        /// </summary>        
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use WithAzureRegion on the ConfidentialClientApplicationBuilder object", true)]
        public AcquireTokenForClientParameterBuilder WithPreferredAzureRegion(bool useAzureRegion = true, string regionUsedIfAutoDetectFails = "", bool fallbackToGlobal = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Specifies an identity attribute to include in the token request.
        /// The attribute values will be returned as a claim in the token called "xms_attr"
        /// in the access token claims. This is typically used with FMI (Federated Managed Identity) scenarios.
        /// Example attribute json: "{\"sg1\":\"0000-00000-0001\",\"sg2\":[\"0000-00000-0002\",\"0000-00000-0003\",\"0000-00000-0004\"]}"
        /// </summary>
        /// <param name="attributeJson">The attribute value to include in the request</param>
        /// <returns>The builder to chain method calls</returns>
        /// <remarks>
        /// The attribute value is included in the cache key, so different attribute values will result in different cache entries.
        /// This ensures that tokens with different attributes are not confused with each other.
        /// </remarks>
        public AcquireTokenForClientParameterBuilder WithAttributes(string attributeJson)
        {
            if (string.IsNullOrWhiteSpace(attributeJson))
            {
                throw new ArgumentNullException(nameof(attributeJson));
            }
            var extraBodyParams = new Dictionary<string, Func<CancellationToken, Task<string>>>
            {
                { OAuth2Parameter.Attributes, _ => Task.FromResult(attributeJson) }
            };

            this.WithExtraBodyParameters(extraBodyParams);

            return this;
        }

        /// <summary> 
        /// Adds an fmi_path parameter to the request. It modifies the subject of the token. 
        /// </summary>
        public AcquireTokenForClientParameterBuilder WithFmiPath(string pathSuffix)
        {
           if (string.IsNullOrWhiteSpace(pathSuffix))
            {
                throw new ArgumentNullException(nameof(pathSuffix));
            }

            var cacheKey = new SortedList<string, Func<CancellationToken, Task<string>>>
            { 
                { OAuth2Parameter.FmiPath, (CancellationToken ct) => {return Task.FromResult(pathSuffix);} } 
            };

            this.WithAdditionalCacheKeyComponents(cacheKey);

            CommonParameters.FmiPathSuffix = pathSuffix;

            return this;
        }

        /// <inheritdoc/>
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc/>
        /// <seealso cref="ConfidentialClientApplicationBuilder.Validate"/> for a comment inside this function for AzureRegion.
        protected override void Validate()
        {
            // Derive "mTLS requested" from "mTLS PoP requested"
            CommonParameters.IsMtlsRequested |= CommonParameters.IsMtlsPopRequested;

            if (CommonParameters.MtlsCertificate != null)
            {
                // Check for Azure region only if the authority is AAD
                // AzureRegion is by default set to null or set to null when the application is created
                // with region set to DisableForceRegion (see ConfidentialClientApplicationBuilder.Validate)
                if (ServiceBundle.Config.Authority.AuthorityInfo.AuthorityType == AuthorityType.Aad &&
                    ServiceBundle.Config.AzureRegion == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsPopWithoutRegion,
                        MsalErrorMessage.MtlsPopWithoutRegion);
                }
            }

            base.Validate();

            // Force refresh + AccessTokenHashToRefresh APIs cannot be used together
            if (Parameters.ForceRefresh && !string.IsNullOrEmpty(Parameters.AccessTokenHashToRefresh))
            {
                throw new MsalClientException(
                    MsalError.ForceRefreshNotCompatibleWithTokenHash,
                    MsalErrorMessage.ForceRefreshAndTokenHasNotCompatible);
            }

            if (Parameters.SendX5C == null)
            {
                Parameters.SendX5C = this.ServiceBundle.Config.SendX5C;
            }
        }

        /// <inheritdoc/>
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenForClient;
        }
    }
}
