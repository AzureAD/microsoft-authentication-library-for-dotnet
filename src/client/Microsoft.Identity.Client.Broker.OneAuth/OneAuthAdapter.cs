// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Authentication;
using Microsoft.OneAuthInterop;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Features.OneAuthBroker
{
    /// <summary>
    /// Adapter layer to wrap OneAuth C# projections and provide compatibility with existing MSAL.NET broker interface
    /// </summary>
    internal class OneAuthAdapter : IBroker, IDisposable
    {
        private readonly ILoggerAdapter _logger;
        private readonly OneAuthCs _oneAuth;
        private bool _initialized = false;

        public OneAuthAdapter(ILoggerAdapter logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _oneAuth = new OneAuthCs();
        }

        public bool IsInitialized => _initialized;

        public bool IsPopSupported => false; // OneAuth broker doesn't support PoP tokens yet

        /// <summary>
        /// Initialize OneAuth with the given configuration
        /// </summary>
        public bool Initialize(
            string clientId,
            string redirectUri,
            string applicationName = null)
        {
            try
            {
                // TODO: Implement proper OneAuth initialization with real config objects
                // For now, just mark as initialized to allow SignInInteractively to work
                _initialized = true;
                _logger?.Info("[OneAuth] Adapter initialized (basic implementation - needs real OneAuth config)");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Failed to initialize: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Shutdown OneAuth
        /// </summary>
        public void Shutdown()
        {
            try
            {
                if (_initialized)
                {
                    _oneAuth?.Shutdown();
                    _initialized = false;
                    _logger?.Info("[OneAuth] OneAuth shutdown completed");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Failed to shutdown: {ex}");
            }
        }

        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            // For now, return false to indicate OneAuth is not ready yet
            return false;
        }

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            _logger?.Info("[OneAuth] AcquireTokenInteractiveAsync called - not yet implemented");
            // TODO: Implement OneAuth silent token acquisition
            return await Task.FromResult<MsalTokenResponse>(null).ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            _logger?.Info("[OneAuth] AcquireTokenSilentAsync called - not yet implemented");
            // TODO: Implement OneAuth silent token acquisition
            return await Task.FromResult<MsalTokenResponse>(null).ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            _logger?.Info("[OneAuth] AcquireTokenSilentDefaultUserAsync called - not yet implemented");
            // TODO: Implement OneAuth silent token acquisition for default user
            return await Task.FromResult<MsalTokenResponse>(null).ConfigureAwait(false);
        }

        [Obsolete("This API has been deprecated, use a more secure flow. See https://aka.ms/msal-ropc-migration for migration guidance", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public async Task<MsalTokenResponse> AcquireTokenByUsernamePasswordAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByUsernamePasswordParameters acquireTokenByUsernamePasswordParameters)
        {
            _logger?.Info("[OneAuth] AcquireTokenByUsernamePasswordAsync called - not supported");
            // OneAuth doesn't support username/password flow
            return await Task.FromResult<MsalTokenResponse>(null).ConfigureAwait(false);
        }

        public IReadOnlyDictionary<string, string> GetSsoPolicyHeaders()
        {
            _logger?.Info("[OneAuth] GetSsoPolicyHeaders called - not yet implemented");
            // TODO: Implement OneAuth GetSsoPolicyHeaders
            return CollectionHelpers.GetEmptyDictionary<string, string>();
        }

        public async Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientId,
            string redirectUri,
            AuthorityInfo authorityInfo,
            ICacheSessionManager cacheSessionManager,
            IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            _logger?.Info("[OneAuth] GetAccountsAsync called - not yet implemented");
            // TODO: Implement OneAuth account enumeration
            return await Task.FromResult(CollectionHelpers.GetEmptyReadOnlyList<IAccount>()).ConfigureAwait(false);
        }

        public async Task RemoveAccountAsync(ApplicationConfiguration appConfig, IAccount account)
        {
            _logger?.Info($"[OneAuth] RemoveAccountAsync called for account: {account?.Username} - not yet implemented");
            // TODO: Implement OneAuth account removal
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> SignInInteractivelyAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters = null)
        {
            try
            {
                if (!_initialized)
                {
                    throw new InvalidOperationException("OneAuth adapter is not initialized");
                }

                _logger?.Info("[OneAuth] Calling SignInInteractively using OneAuth C# API");

                // Create UxContext for OneAuth UI handling
                var uxContext = new UxContext(IntPtr.Zero, IntPtr.Zero, "OneAuth");

                // Create account hint
                string accountHint = authenticationRequestParameters.LoginHint ?? authenticationRequestParameters?.Account?.Username;

                // Convert MSAL parameters to OneAuth AuthParameters (actual OneAuth package type)
                var authenticationParameters = OneAuthParameterMappers.ToOneAuthAuthParameters(
                    authenticationRequestParameters, 
                    acquireTokenInteractiveParameters);

                // Convert MSAL parameters to OneAuth SignInBehaviorParameters
                var signInBehaviorParameters = OneAuthParameterMappers.ToOneAuthSignInBehaviorParameters(acquireTokenInteractiveParameters);

                // Create TelemetryParameters for OneAuth
                var telemetryParameters = new TelemetryParameters(
                    "OneAuth", 
                    "SignInInteractively", 
                    authenticationRequestParameters.CorrelationId.ToString("D"));

                // Log what we're about to send to OneAuth (using internal representation for logging)
                var internalAuthParams = OneAuthParameterMappers.ToOneAuthInternalAuthParameters(
                    authenticationRequestParameters, 
                    acquireTokenInteractiveParameters);

                _logger?.Info($"[OneAuth] Calling OneAuth SignInInteractively with:");
                _logger?.Info($"[OneAuth] - Authority: {internalAuthParams.Authority}");
                _logger?.Info($"[OneAuth] - Target: {internalAuthParams.Target}");
                _logger?.Info($"[OneAuth] - AuthenticationScheme: {internalAuthParams.AuthenticationScheme}");
                _logger?.Info($"[OneAuth] - Claims: {internalAuthParams.Claims}");
                _logger?.Info($"[OneAuth] - AccountHint: {accountHint ?? "N/A"}");
                _logger?.Info($"[OneAuth] - AdditionalParameters count: {internalAuthParams.AdditionalParameters?.Count ?? 0}");
                _logger?.Info($"[OneAuth] - Capabilities count: {internalAuthParams.Capabilities?.Count ?? 0}");

                // Call OneAuth SignInInteractively with the correct signature
                var authResult = await _oneAuth.SignInInteractively(
                    uxContext,
                    accountHint,
                    authenticationParameters,  // Now using proper OneAuth AuthParameters type
                    signInBehaviorParameters,
                    telemetryParameters).ConfigureAwait(false);

                // Convert OneAuth result to MSAL token response
                return ConvertOneAuthResultToMsalTokenResponse(authResult, authenticationRequestParameters);
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Interactive authentication failed: {ex}");
                return new MsalTokenResponse
                {
                    Error = MsalError.UnknownBrokerError,
                    ErrorDescription = ex.Message,
                    CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
                };
            }
        }

        /// <summary>
        /// Converts OneAuth authentication result to MSAL token response
        /// </summary>
        private MsalTokenResponse ConvertOneAuthResultToMsalTokenResponse(
            AuthResult authResult,
            AuthenticationRequestParameters authenticationRequestParameters)
        {
            if (authResult == null)
            {
                return new MsalTokenResponse
                {
                    Error = MsalError.UnknownBrokerError,
                    ErrorDescription = "OneAuth returned null result",
                    CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
                };
            }

            try
            {
                // Check if OneAuth returned an error
                if (authResult.Error != null)
                {
                    _logger?.Error($"[OneAuth] Authentication failed with error: {authResult.Error}");
                    return new MsalTokenResponse
                    {
                        Error = MapOneAuthErrorToMsalError(authResult.Error.ToString()),
                        ErrorDescription = authResult.Error.ToString(),
                        CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
                    };
                }

                // Convert successful OneAuth result to MSAL token response
                // Note: Property names will need to be adjusted based on actual OneAuth AuthResult structure
                var tokenResponse = new MsalTokenResponse
                {
                    // Map OneAuth result properties to MSAL token response
                    // These property names are assumptions and will need to be corrected based on actual OneAuth AuthResult
                    AccessToken = GetAuthResultProperty(authResult, "AccessToken"),
                    RefreshToken = GetAuthResultProperty(authResult, "RefreshToken"),
                    IdToken = GetAuthResultProperty(authResult, "IdToken"),
                    TokenType = GetAuthResultProperty(authResult, "TokenType") ?? "Bearer",
                    ExpiresIn = GetAuthResultPropertyAsLong(authResult, "ExpiresIn"),
                    Scope = GetAuthResultProperty(authResult, "Scope"),
                    ClientInfo = GetAuthResultProperty(authResult, "ClientInfo"),
                    CorrelationId = authenticationRequestParameters.CorrelationId.ToString(),
                    WamAccountId = GetAuthResultProperty(authResult, "AccountId"),
                    TokenSource = TokenSource.Broker
                };

                _logger?.Info("[OneAuth] Successfully converted OneAuth result to MSAL token response");
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Failed to convert OneAuth result to MSAL token response: {ex}");
                return new MsalTokenResponse
                {
                    Error = MsalError.UnknownBrokerError,
                    ErrorDescription = $"Failed to convert OneAuth result: {ex.Message}",
                    CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
                };
            }
        }

        /// <summary>
        /// Helper method to safely get properties from OneAuth AuthResult
        /// This will be updated based on actual OneAuth AuthResult structure
        /// </summary>
        private string GetAuthResultProperty(AuthResult authResult, string propertyName)
        {
            try
            {
                // Use reflection to get property value until we know the exact AuthResult structure
                var property = authResult.GetType().GetProperty(propertyName);
                return property?.GetValue(authResult)?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Helper method to safely get long properties from OneAuth AuthResult
        /// </summary>
        private long GetAuthResultPropertyAsLong(AuthResult authResult, string propertyName)
        {
            try
            {
                var property = authResult.GetType().GetProperty(propertyName);
                var value = property?.GetValue(authResult);
                if (value != null && long.TryParse(value.ToString(), out long result))
                {
                    return result;
                }
                return 3600; // Default 1 hour
            }
            catch
            {
                return 3600; // Default 1 hour
            }
        }

        /// <summary>
        /// Maps OneAuth errors to MSAL error codes
        /// </summary>
        private string MapOneAuthErrorToMsalError(string oneAuthError)
        {
            if (string.IsNullOrEmpty(oneAuthError))
                return MsalError.UnknownBrokerError;

            // Map common OneAuth errors to MSAL errors
            var errorLower = oneAuthError.ToLowerInvariant();
            
            if (errorLower.Contains("user_cancel") || errorLower.Contains("authentication_canceled"))
                return MsalError.AuthenticationCanceledError;
            
            if (errorLower.Contains("invalid_request"))
                return MsalError.InvalidRequest;
            
            if (errorLower.Contains("invalid_client"))
                return MsalError.InvalidClient;
            
            if (errorLower.Contains("invalid_grant"))
                return MsalError.InvalidGrantError;
            
            if (errorLower.Contains("unauthorized_client"))
                return MsalError.UnauthorizedClient;
            
            if (errorLower.Contains("unsupported_grant_type"))
                return MsalError.InvalidGrantError; // Use available error
            
            if (errorLower.Contains("invalid_scope"))
                return MsalError.InvalidRequest; // Use available error
            
            return MsalError.UnknownBrokerError;
        }

        public void HandleInstallUrl(string appLink)
        {
            _logger?.Info($"[OneAuth] Install URL handled: {appLink}");
            // No action needed for OneAuth
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
