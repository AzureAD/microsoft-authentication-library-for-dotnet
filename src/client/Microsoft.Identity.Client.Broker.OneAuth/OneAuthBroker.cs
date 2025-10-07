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
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.Platforms.Features.OneAuthBroker
{
    /// <summary>
    /// Adapter layer to wrap OneAuth C# projections and provide compatibility with existing MSAL.NET broker interface
    /// </summary>
    internal class OneAuthBroker : IBroker, IDisposable
    {
        private readonly ILoggerAdapter _logger;
        private readonly IntPtr _parentHandle = IntPtr.Zero;
        private readonly BrokerOptions _brokerOptions;
        private readonly OneAuthCs _oneAuth;
        private bool _initialized = false;

        public bool IsInitialized => _initialized;

        public bool IsPopSupported => true; 

        /// <summary>
        /// Initialize OneAuth with the given configuration
        /// </summary>
        public bool Initialize(
            string clientId,
            string redirectUri,
            string applicationName = null)
        {
            // OneAuth initialization now happens in the constructor via Startup call
            // This method now just returns the initialization status
            if (_initialized)
            {
                _logger?.Info($"[OneAuth] OneAuth is initialized and ready for clientId: {clientId}");
                return true;
            }
            else
            {
                _logger?.Warning($"[OneAuth] OneAuth initialization is not yet fully implemented. " +
                    $"Cannot process requests for clientId: {clientId}. " +
                    "Startup method requires specific OneAuth configuration objects that need to be documented.");
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

        private static IntPtr GetParentWindow(CoreUIParent uiParent)
        {
            if (uiParent?.OwnerWindow is IntPtr ptr)
            {
                return ptr;
            }

            return IntPtr.Zero;
        }

        public OneAuthBroker(
            CoreUIParent uiParent,
            ApplicationConfiguration appConfig,
            ILoggerAdapter logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _oneAuth = new OneAuthCs();

            // Set parent window handle for OneAuth UI on Windows
            _parentHandle = GetParentWindow(uiParent);
            
            // Broker options cannot be null
            _brokerOptions = appConfig.BrokerOptions ?? throw new ArgumentNullException(nameof(appConfig.BrokerOptions));

            // Initialize OneAuth by calling Startup with required configuration objects
            InitializeOneAuth(appConfig);
        }

        /// <summary>
        /// Initialize OneAuth by calling the Startup method with proper configuration objects
        /// </summary>
        private void InitializeOneAuth(ApplicationConfiguration appConfig)
        {
            try
            {
                _logger?.Info("[OneAuth] Starting OneAuth initialization via Startup method");

                // For now, create a minimal implementation that attempts to call Startup
                // This will need to be completed once the exact OneAuth constructor requirements are known
                _logger?.Warning("[OneAuth] OneAuth initialization is not yet fully implemented - Startup method requires specific configuration objects");

                // Mark as not initialized until proper configuration objects can be created
                //_initialized = false;

                // TODO: Uncomment and complete once OneAuth package documentation is available

                var oneAuthAppConfig = CreateConfiguredAppConfig(appConfig);
                var aadConfig = CreateConfiguredAadConfig(appConfig);
                var msaConfig = CreateConfiguredMsaConfig(appConfig);
                //var telemetryConfig = ;

                var startupError = _oneAuth.Startup(oneAuthAppConfig, aadConfig, msaConfig, null);

                if (startupError != null)
                {
                    _logger?.Error($"[OneAuth] Startup failed with error: {startupError}");
                    _initialized = false;
                }
                else
                {
                    _initialized = true;
                    _logger?.Info("[OneAuth] OneAuth successfully initialized via Startup method");
                    
                    // Set up logging
                    _oneAuth.SetLogPiiEnabled(_logger.PiiLoggingEnabled);
                    _oneAuth.SetLogCallback((level, message, identifiableInfo) =>
                    {
                        _logger?.Info($"[OneAuth] {message}");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Failed to initialize OneAuth: {ex}");
                _initialized = false;
            }
        }

        // TODO: Implement these methods once OneAuth constructor requirements are documented
        private Microsoft.OneAuthInterop.AppConfig CreateConfiguredAppConfig(ApplicationConfiguration appConfig)
        {
            return new Microsoft.OneAuthInterop.AppConfig(
                appId: appConfig.ClientId,                    // string appId
                appName: "MSAL.NET OneAuth Broker",          // string appName  
                appVersion: "1.0.0",                         // string appVersion
                languageCode: "en-US"                        // string languageCode
                                                             // Optional: hrdAppId parameter for the second constructor
            );
        }

        private Microsoft.OneAuthInterop.AadConfig CreateConfiguredAadConfig(ApplicationConfiguration appConfig)
        {
            var aadConfig = new Microsoft.OneAuthInterop.AadConfig(
                clientId: appConfig.ClientId,                        // string clientId
                redirectUri: appConfig.RedirectUri,                  // string redirectUri
                defaultSignInResource: "https://graph.microsoft.com/.default", // string defaultSignInResource
                capabilities: appConfig.ClientCapabilities?.ToList() ?? new List<string>(), // List<string> capabilities
                allowSameRealm: true                                // bool allowSameRealm
            );

            return aadConfig;
        }

        private Microsoft.OneAuthInterop.MsaConfig CreateConfiguredMsaConfig(ApplicationConfiguration appConfig)
        {
            // MSA (Microsoft Account) default sign-in scope
            // For MSA scenarios, we typically use basic profile scopes or Microsoft Graph
            var defaultSignInScope = "https://graph.microsoft.com/User.Read";

            var msaConfig = new Microsoft.OneAuthInterop.MsaConfig(
                clientId: appConfig.ClientId,                        // string clientId
                redirectUri: appConfig.RedirectUri,                  // string redirectUri
                defaultSignInScope: defaultSignInScope               // string defaultSignInScope
            );

            return msaConfig;
        }

        private Microsoft.OneAuthInterop.TelemetryConfig CreateConfiguredTelemetryConfig(ApplicationConfiguration appConfig)
        {
            // Will be implemented based on actual OneAuth TelemetryConfig constructor requirements
            throw new NotImplementedException("OneAuth TelemetryConfig creation not yet implemented");
        }

        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            // For now, return false to indicate OneAuth is not ready yet
            return true;
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
                var uxContext = new UxContext(_parentHandle, IntPtr.Zero, "OneAuth");

                // Create account hint
                string accountHint = authenticationRequestParameters.LoginHint ?? authenticationRequestParameters?.Account?.Username;

                // Create TelemetryParameters for OneAuth
                var telemetryParameters = new TelemetryParameters(
                    "OneAuth", 
                    "SignInInteractively", 
                    authenticationRequestParameters.CorrelationId.ToString("D"));

                // Log what we're about to send to OneAuth (using internal representation for logging)
                var oneAuthParams = OneAuthParameterMappers.CreateDirectOneAuthParameters(
                    authenticationRequestParameters,
                    _logger);

                // Call OneAuth SignInInteractively with the correct signature
                var authResult = await _oneAuth.SignInInteractively(
                    uxContext,
                    accountHint,
                    oneAuthParams,  // ← Uses actual OneAuth AuthParameters!
                    null,
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
