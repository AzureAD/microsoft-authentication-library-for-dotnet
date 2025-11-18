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
using Microsoft.Authentication.Client;
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
        private readonly ApplicationConfiguration _appConfig;
        //private readonly Authenticator _oneAuth;

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
                    Authenticator.Shutdown();
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
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            //_oneAuth = new OneAuthCs();

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
            _initialized = true;
            return;
            //try
            //{
            //    _logger?.Info("[OneAuth] Starting OneAuth initialization via Startup method");

            //    // For now, create a minimal implementation that attempts to call Startup
            //    // This will need to be completed once the exact OneAuth constructor requirements are known
            //    _logger?.Warning("[OneAuth] OneAuth initialization is not yet fully implemented - Startup method requires specific configuration objects");

            //    // Mark as not initialized until proper configuration objects can be created
            //    _initialized = false;

            //    var oneAuthAppConfig = CreateConfiguredAppConfig(appConfig);
            //    var aadConfig = CreateConfiguredAadConfig(appConfig);
            //    var msaConfig = CreateConfiguredMsaConfig(appConfig);
            //    //var telemetryConfig = ;
            //    //var applConfig = new Microsoft.OneAuthInterop.AppConfig(
            //    //    appId: "com.microsoft.OneAuthDotNetTest",
            //    //    appName: "OneAuthTest",
            //    //    appVersion: "0.0.0",
            //    //    languageCode: "en"
            //    //);

            //    //var aadConfig = new Microsoft.OneAuthInterop.AadConfig
            //    //(
            //    //    clientId: "d3590ed6-52b3-4102-aeff-aad2292ab01c",
            //    //    redirectUri: "urn:ietf:wg:oauth:2.0:oob",
            //    //    defaultSignInResource: "https://graph.microsoft.com",
            //    //    capabilities: appConfig.ClientCapabilities?.ToList() ?? new List<string>(), // List<string> capabilities
            //    //    allowSameRealm: true
            //    //);

            //    //var msaConfig = new Microsoft.OneAuthInterop.MsaConfig
            //    //(
            //    //    clientId: "00000000480728C5",
            //    //    redirectUri: "https://login.live.com/oauth20_desktop.srf",
            //    //    defaultSignInScope: "https://graph.microsoft.com/.default"
            //    //);

            //    var startupError = _oneAuth.Startup(oneAuthAppConfig, aadConfig, msaConfig, null);

            //    if (startupError != null)
            //    {
            //        _logger?.Error($"[OneAuth] Startup failed with error: {startupError}");
            //        _initialized = false;
            //    }
            //    else
            //    {
            //        _initialized = true;
            //        _logger?.Info("[OneAuth] OneAuth successfully initialized via Startup method");

            //        // Set up logging
            //        _oneAuth.SetLogPiiEnabled(_logger.PiiLoggingEnabled);
            //        _oneAuth.SetLogCallback((level, message, identifiableInfo) =>
            //        {
            //            _logger?.Info($"[OneAuth] {message}");
            //        });
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger?.Error($"[OneAuth] Failed to initialize OneAuth: {ex}");
            //    _initialized = false;
            //}
        }

        private Microsoft.Authentication.Client.AppConfiguration CreateConfiguredAppConfig(ApplicationConfiguration appConfig)
        {
            return new Microsoft.Authentication.Client.AppConfiguration
            {
                ApplicationId = appConfig.ClientId,                    // string ApplicationId
                AppName = "MSAL.NET OneAuth Broker",                   // string AppName  
                AppVersion = "1.0.0",                                  // string AppVersion
                LanguageCode = "en-US"                                 // string LanguageCode
                // Optional: HrdApplicationId can be set if needed
            };
        }

        private Microsoft.Authentication.Client.AadConfiguration CreateConfiguredAadConfig(ApplicationConfiguration appConfig)
        {
            var aadConfig = new Microsoft.Authentication.Client.AadConfiguration
            {
                ClientId = Guid.TryParse(appConfig.ClientId, out var guid) ? guid : Guid.Empty,
                RedirectUri = appConfig.RedirectUri,
                DefaultSignInResource = "https://graph.microsoft.com/.default",
                Capabilities = appConfig.ClientCapabilities?.ToList() ?? new List<string>(),
                AllowSameRealmAccount = true
            };

            return aadConfig;
        }

        private Microsoft.Authentication.Client.MsaConfiguration CreateConfiguredMsaConfig(ApplicationConfiguration appConfig)
        {
            // MSA (Microsoft Account) default sign-in scope
            // For MSA scenarios, we typically use basic profile scopes or Microsoft Graph
            var defaultSignInScope = "https://graph.microsoft.com/User.Read";

            var msaConfig = new Microsoft.Authentication.Client.MsaConfiguration
            {
                ClientId = appConfig.ClientId,                        // string clientId
                RedirectUri = appConfig.RedirectUri,                  // string redirectUri
                DefaultSignInScope = defaultSignInScope               // string defaultSignInScope
            };

            return msaConfig;
        }

        private Microsoft.Authentication.Client.TelemetryConfiguration CreateConfiguredTelemetryConfig(ApplicationConfiguration appConfig)
        {
            // Will be implemented based on actual OneAuth TelemetryConfig constructor requirements
            throw new NotImplementedException("OneAuth TelemetryConfig creation not yet implemented");
        }

        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            // For now, return false to indicate OneAuth is not ready yet
            return true;
        }

        public async Task SignInTest()
        {
            var appConfig = new Microsoft.Authentication.Client.AppConfiguration
            {
                ApplicationId = "com.microsoft.OneAuthDotNetTest",
                AppName = "OneAuthTest",
                AppVersion = "0.0.0",
                LanguageCode = "en"
            };

            var aadConfig = new Microsoft.Authentication.Client.AadConfiguration
            {
                ClientId = Guid.Parse("d3590ed6-52b3-4102-aeff-aad2292ab01c"),
                RedirectUri = "urn:ietf:wg:oauth:2.0:oob",
                DefaultSignInResource = "https://graph.microsoft.com",
                Capabilities = new List<string>(), // List<string> capabilities
                AllowSameRealmAccount = true
            };

            var msaConfig = new Microsoft.Authentication.Client.MsaConfiguration
            {
                ClientId = "00000000480728C5",
                RedirectUri = "https://login.live.com/oauth20_desktop.srf",
                DefaultSignInScope = "https://graph.microsoft.com/.default"
            };

            await Authenticator.Startup(appConfig, aadConfig, msaConfig, null);
            //if (startupError != null)
            //{
            //    _logger?.Error($"[OneAuth] SignInTest startup failed: {startupError}");
            //    return;
            //}

            //var telemetryParameters = new Microsoft.Authentication.Client.TelemetryParameters(
            //    "OneAuth",
            //    "SignInTest",
            //    "fsdfsgsagsagas");

            var authResult = await Authenticator.SignInInteractively(
                uxContext: Microsoft.Authentication.Client.UxContext.Default,
                accountHint: "",
                authenticationParameters: null,
                signInBehaviorParameters: null,
                telemetryParameters: new TelemetryParameters()
            );

            _logger?.Info($"[OneAuth] SignInTest completed with result: {authResult}");
            Authenticator.Shutdown();

        }
        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            _logger?.Info("[OneAuth] AcquireTokenInteractiveAsync called");

            // Use TaskCompletionSource to properly handle the STA thread requirement
            var tcs = new TaskCompletionSource<MsalTokenResponse>();
            
            Thread thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    // Invoke SignInTest to test OneAuth functionality (commented out)
                    // SignInTest().GetAwaiter().GetResult();
                    
                    // Run SignInInteractivelyAsync method synchronously on the STA thread
                    var result = SignInInteractivelyAsync(
                        authenticationRequestParameters,
                        acquireTokenInteractiveParameters).GetAwaiter().GetResult();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    _logger?.Error($"[OneAuth] Exception in STA thread: {ex}");
                    tcs.SetException(ex);
                }           
            }))
            {
                IsBackground = false
            };
            
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(); // Wait for the thread to complete to ensure proper cleanup
            
            return await tcs.Task.ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            //if (!_initialized)
            //{
            //    _logger?.Warning("[OneAuth] OneAuth is not initialized. Cannot acquire token silently.");
            //    return new MsalTokenResponse
            //    {
            //        Error = MsalError.UnknownBrokerError,
            //        ErrorDescription = "OneAuth is not initialized",
            //        CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
            //    };
            //}

            try
            {
                _logger?.Info("[OneAuth] Acquiring token silently using ReadAccountById and AcquireCredentialSilently");

                var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;

                // Initialize OneAuth before acquiring token
                var oneAuthAppConfig = CreateConfiguredAppConfig(_appConfig);
                var aadConfig = CreateConfiguredAadConfig(_appConfig);
                var msaConfig = CreateConfiguredMsaConfig(_appConfig);
                
                await Authenticator.Startup(oneAuthAppConfig, aadConfig, msaConfig, null);

                // Create telemetry parameters
                var telemetryParameters = new TelemetryParameters();

                // Read the account by ID using OneAuth API
                var oneAuthAccount = await Authenticator.ReadAccountById(
                    authenticationRequestParameters.Account.HomeAccountId.ObjectId,
                    telemetryParameters).ConfigureAwait(false);

                if (oneAuthAccount == null)
                {
                    _logger?.WarningPii(
                        $"[OneAuth] Could not find a OneAuth account for the selected user {acquireTokenSilentParameters.Account.Username}.",
                        "[OneAuth] Could not find a OneAuth account for the selected user.");

                    Authenticator.Shutdown();

                    throw new MsalUiRequiredException(
                        "oneauth_no_account_for_id",
                        "Could not find a OneAuth account for the selected user.");
                }

                _logger?.Verbose(() => $"[OneAuth] Found account: {oneAuthAccount.Id}");

                // Create authentication parameters for silent token acquisition
                var oneAuthParams = OneAuthParameterMappers.CreateDirectOneAuthParameters(
                    authenticationRequestParameters,
                    _logger);

                MsalTokenResponse msalTokenResponse;

                // Acquire credential silently using OneAuth API
                AuthResult result = await Authenticator.AcquireCredentialSilently(
                    oneAuthAccount,
                    oneAuthParams,
                    telemetryParameters).ConfigureAwait(false);

                _logger?.Verbose(() => "[OneAuth] AcquireCredentialSilently completed");
                msalTokenResponse = ConvertOneAuthResultToMsalTokenResponse(result, authenticationRequestParameters, _logger);

                // Handle ForceRefresh scenario
                if (acquireTokenSilentParameters.ForceRefresh && !string.IsNullOrEmpty(msalTokenResponse.AccessToken))
                {
                    _logger?.Verbose(() => "[OneAuth] ForceRefresh requested, acquiring token again with AccessTokenToRenew");

                    // Set the access token to renew for force refresh
                    oneAuthParams.AccessTokenToRenew = msalTokenResponse.AccessToken;

                    result = await Authenticator.AcquireCredentialSilently(
                        oneAuthAccount,
                        oneAuthParams,
                        telemetryParameters).ConfigureAwait(false);

                    _logger?.Verbose(() => "[OneAuth] AcquireCredentialSilently with ForceRefresh completed");
                    msalTokenResponse = ConvertOneAuthResultToMsalTokenResponse(result, authenticationRequestParameters, _logger);
                }

                // Shutdown Authenticator after token acquisition
                Authenticator.Shutdown();

                _logger?.Info("[OneAuth] Silent token acquisition completed successfully");
                return msalTokenResponse;
            }
            catch (MsalUiRequiredException)
            {
                // Re-throw UI required exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Failed to acquire token silently: {ex}");
                
                // Ensure cleanup on error
                try
                {
                    Authenticator.Shutdown();
                }
                catch (Exception shutdownEx)
                {
                    _logger?.Warning($"[OneAuth] Failed to shutdown Authenticator after error: {shutdownEx}");
                }

                return new MsalTokenResponse
                {
                    Error = MsalError.UnknownBrokerError,
                    ErrorDescription = ex.Message,
                    CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
                };
            }
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            if (!_initialized)
            {
                _logger?.Warning("[OneAuth] OneAuth is not initialized. Cannot acquire token silently for default user.");
                return new MsalTokenResponse
                {
                    Error = MsalError.UnknownBrokerError,
                    ErrorDescription = "OneAuth is not initialized",
                    CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
                };
            }

            try
            {
                _logger?.Info("[OneAuth] Acquiring token silently for default user using SignInSilently");

                var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;

                // Initialize OneAuth before acquiring token
                var oneAuthAppConfig = CreateConfiguredAppConfig(_appConfig);
                var aadConfig = CreateConfiguredAadConfig(_appConfig);
                var msaConfig = CreateConfiguredMsaConfig(_appConfig);
                
                await Authenticator.Startup(oneAuthAppConfig, aadConfig, msaConfig, null);

                // Create authentication parameters for silent token acquisition
                var oneAuthParams = OneAuthParameterMappers.CreateDirectOneAuthParameters(
                    authenticationRequestParameters,
                    _logger);

                // Create telemetry parameters
                var telemetryParameters = new TelemetryParameters();

                MsalTokenResponse msalTokenResponse;

                // Sign in silently using default OS user - uses SignInSilently API
                AuthResult result = await Authenticator.SignInSilently(
                    oneAuthParams,
                    telemetryParameters).ConfigureAwait(false);

                _logger?.Verbose(() => "[OneAuth] SignInSilently completed");
                msalTokenResponse = ConvertOneAuthResultToMsalTokenResponse(result, authenticationRequestParameters, _logger);

                // Handle ForceRefresh scenario
                if (acquireTokenSilentParameters.ForceRefresh && !string.IsNullOrEmpty(msalTokenResponse.AccessToken))
                {
                    _logger?.Verbose(() => "[OneAuth] ForceRefresh requested for default user, acquiring token again with AccessTokenToRenew");

                    // Set the access token to renew for force refresh
                    oneAuthParams.AccessTokenToRenew = msalTokenResponse.AccessToken;

                    result = await Authenticator.SignInSilently(
                        oneAuthParams,
                        telemetryParameters).ConfigureAwait(false);

                    _logger?.Verbose(() => "[OneAuth] SignInSilently with ForceRefresh completed");
                    msalTokenResponse = ConvertOneAuthResultToMsalTokenResponse(result, authenticationRequestParameters, _logger);
                }

                // Shutdown Authenticator after token acquisition
                Authenticator.Shutdown();

                _logger?.Info("[OneAuth] Silent token acquisition for default user completed successfully");
                return msalTokenResponse;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Failed to acquire token silently for default user: {ex}");
                
                // Ensure cleanup on error
                try
                {
                    Authenticator.Shutdown();
                }
                catch (Exception shutdownEx)
                {
                    _logger?.Warning($"[OneAuth] Failed to shutdown Authenticator after error: {shutdownEx}");
                }

                return new MsalTokenResponse
                {
                    Error = MsalError.UnknownBrokerError,
                    ErrorDescription = ex.Message,
                    CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
                };
            }
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
            //if (!_brokerOptions.ListOperatingSystemAccounts)
            //{
            //    _logger?.Info("[OneAuth] ListOperatingSystemAccounts option is not enabled.");
            //    return Array.Empty<IAccount>();
            //}

            //if (!_initialized)
            //{
            //    _logger?.Warning("[OneAuth] OneAuth is not initialized. Cannot retrieve accounts.");
            //    return Array.Empty<IAccount>();
            //}

            try
            {
                _logger?.Info("[OneAuth] Starting account discovery using DiscoverAccounts API");

                var requestContext = cacheSessionManager.RequestContext;
                var cancellationToken = requestContext.UserCancellationToken;

                // Initialize OneAuth before discovering accounts
                var oneAuthAppConfig = CreateConfiguredAppConfig(_appConfig);
                var aadConfig = CreateConfiguredAadConfig(_appConfig);
                var msaConfig = CreateConfiguredMsaConfig(_appConfig);
                
                await Authenticator.Startup(oneAuthAppConfig, aadConfig, msaConfig, null);

                // Create discovery parameters
                var discoveryParameters = new DiscoveryParameters();
                var telemetryParameters = new TelemetryParameters();

                // Collect all discovered accounts
                var oneAuthAccounts = new List<Microsoft.Authentication.Client.Account>();

                // Use DiscoverAccounts API which returns IAsyncEnumerable<DiscoveryResult>
                await foreach (var discoveryResult in Authenticator.DiscoverAccounts(discoveryParameters, telemetryParameters))
                {
                    if (discoveryResult.Account != null && discoveryResult.DiscoveryStatus != DiscoveryStatus.None)
                    {
                        oneAuthAccounts.Add(discoveryResult.Account);
                        _logger?.Verbose(() => $"[OneAuth] Discovered account: {discoveryResult.Account.Id}");
                    }
                }

                _logger?.Info(() => $"[OneAuth] Discovery completed. Found {oneAuthAccounts.Count} account(s).");

                if (oneAuthAccounts.Count == 0)
                {
                    Authenticator.Shutdown();
                    return Array.Empty<IAccount>();
                }

                // Filter accounts based on environment if multi-cloud is not enabled
                if (!requestContext.ServiceBundle.Config.MultiCloudSupportEnabled)
                {
                    var environmentList = oneAuthAccounts
                        .Where(acc => !string.IsNullOrEmpty(acc.Environment))
                        .Select(acc => acc.Environment)
                        .Distinct()
                        .ToList();

                    if (environmentList.Any())
                    {
                        var instanceMetadata = await instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                            authorityInfo,
                            environmentList,
                            requestContext).ConfigureAwait(false);

                        _logger?.Verbose(() => "[OneAuth] Filtering accounts based on Environment.");

                        oneAuthAccounts.RemoveAll(acc => 
                            string.IsNullOrEmpty(acc.Environment) || 
                            !instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(acc.Environment));

                        _logger?.Verbose(() => $"[OneAuth] {oneAuthAccounts.Count} account(s) returned after filtering.");
                    }
                }

                // Convert OneAuth accounts to MSAL accounts
                List<IAccount> msalAccounts = new List<IAccount>();

                foreach (var oneAuthAccount in oneAuthAccounts)
                {
                    if (TryConvertOneAuthAccountToMsalAccount(oneAuthAccount, clientId, _logger, out IAccount msalAccount))
                    {
                        msalAccounts.Add(msalAccount);
                    }
                }

                _logger?.Verbose(() => $"[OneAuth] Converted {msalAccounts.Count} OneAuth account(s) to MSAL Account(s).");

                // Shutdown Authenticator after account discovery
                Authenticator.Shutdown();

                return msalAccounts;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Failed to discover accounts: {ex}");
                
                // Ensure cleanup on error
                try
                {
                    Authenticator.Shutdown();
                }
                catch (Exception shutdownEx)
                {
                    _logger?.Warning($"[OneAuth] Failed to shutdown Authenticator after error: {shutdownEx}");
                }

                return Array.Empty<IAccount>();
            }
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

                // Create OneAuth configurations using the stored app config
                var oneAuthAppConfig = CreateConfiguredAppConfig(_appConfig);
                var aadConfig = CreateConfiguredAadConfig(_appConfig);
                var msaConfig = CreateConfiguredMsaConfig(_appConfig);
                
                // Call Authenticator.Startup with the configured parameters
                await Authenticator.Startup(oneAuthAppConfig, aadConfig, msaConfig, null);
                
                _logger?.Info("[OneAuth] Calling SignInInteractively using OneAuth C# API");

                // Create UxContext for OneAuth UI handling
                var uxContext = new UxContext(_parentHandle, IntPtr.Zero, "OneAuth");

                // Create account hint
                //string accountHint = authenticationRequestParameters.LoginHint ?? authenticationRequestParameters?.Account?.Username;

                // Create TelemetryParameters for OneAuth
                //var telemetryParameters = new Microsoft.Authentication.Client.TelemetryParameters(
                //    "OneAuth", 
                //    "SignInInteractively", 
                //    authenticationRequestParameters.CorrelationId.ToString("D"));

                // Log what we're about to send to OneAuth (using internal representation for logging)
                var oneAuthParams = OneAuthParameterMappers.CreateDirectOneAuthParameters(
                    authenticationRequestParameters,
                    _logger);

                // Call OneAuth directly since we're already running on STA thread
                AuthResult authResult = await Authenticator.SignInInteractively(
                    uxContext,
                    "",
                    oneAuthParams,
                    null,
                    telemetryParameters: new TelemetryParameters()).ConfigureAwait(false);

                // Convert OneAuth result to MSAL token response
                var result = ConvertOneAuthResultToMsalTokenResponse(authResult, authenticationRequestParameters, _logger);
                
                // Shutdown Authenticator after completing the authentication
                Authenticator.Shutdown();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[OneAuth] Interactive authentication failed: {ex}");
                
                // Ensure Authenticator is shutdown even on error
                try
                {
                    Authenticator.Shutdown();
                }
                catch (Exception shutdownEx)
                {
                    _logger?.Warning($"[OneAuth] Failed to shutdown Authenticator after error: {shutdownEx}");
                }
                
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
            AuthenticationRequestParameters authenticationRequestParameters,
            ILoggerAdapter logger)
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
                    logger?.Error($"[OneAuth] Authentication failed with error: {authResult.Error}");
                    return new MsalTokenResponse
                    {
                        Error = MapOneAuthErrorToMsalError(authResult.Error.ToString()),
                        ErrorDescription = authResult.Error.ToString(),
                        CorrelationId = authenticationRequestParameters.CorrelationId.ToString()
                    };
                }

                // Convert successful OneAuth result to MSAL token response
                string correlationId = authenticationRequestParameters.CorrelationId.ToString("D");
                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    logger.Warning("[RuntimeBroker] No correlation ID in response");
                    correlationId = null;
                }

                string authorityUrl = null;
                if (authenticationRequestParameters.AppConfig.MultiCloudSupportEnabled)
                {
                    IdToken idToken = IdToken.Parse(authResult.Credential.Id);
                    authorityUrl = idToken.ClaimsPrincipal.FindFirst("iss")?.Value;
                    if (authorityUrl.EndsWith("v2.0"))
                        authorityUrl = authorityUrl.Substring(0, authorityUrl.Length - "v2.0".Length);
                }

                MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
                {
                    AuthorityUrl = authorityUrl,
                    AccessToken = authResult.Credential.Secret,
                    //IdToken = authResult.Credential.Id,
                    CorrelationId = correlationId,
                    Scope = authResult.Credential.Target,
                    //ExpiresIn = (long)(DateTime.SpecifyKind(authResult.Credential.ExpiresOn, DateTimeKind.Utc) - DateTimeOffset.UtcNow).TotalSeconds,
                    //ClientInfo = authResult.Account.ClientInfo,
                    TokenType = authenticationRequestParameters.AuthenticationScheme.AuthorizationHeaderPrefix,
                    WamAccountId = authResult.Account.Id,
                    TokenSource = TokenSource.Broker
                };
                //var tokenResponse = new MsalTokenResponse
                //{
                //    // Map OneAuth result properties to MSAL token response
                //    // These property names are assumptions and will need to be corrected based on actual OneAuth AuthResult
                //    AccessToken = GetAuthResultProperty(authResult, "AccessToken"),
                //    RefreshToken = GetAuthResultProperty(authResult, "RefreshToken"),
                //    IdToken = GetAuthResultProperty(authResult, "IdToken"),
                //    TokenType = GetAuthResultProperty(authResult, "TokenType") ?? "Bearer",
                //    ExpiresIn = GetAuthResultPropertyAsLong(authResult, "ExpiresIn"),
                //    Scope = GetAuthResultProperty(authResult, "Scope"),
                //    ClientInfo = GetAuthResultProperty(authResult, "ClientInfo"),
                //    CorrelationId = authenticationRequestParameters.CorrelationId.ToString(),
                //    WamAccountId = GetAuthResultProperty(authResult, "AccountId"),
                //    TokenSource = TokenSource.Broker
                //};

                _logger?.Info("[OneAuth] Successfully converted OneAuth result to MSAL token response");
                return msalTokenResponse;
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
        // private string GetAuthResultProperty(AuthResult authResult, string propertyName)
        // {
        //     try
        //     {
        //         // Use reflection to get property value until we know the exact AuthResult structure
        //         var property = authResult.GetType().GetProperty(propertyName);
        //         return property?.GetValue(authResult)?.ToString();
        //     }
        //     catch
        //     {
        //         return null;
        //     }
        // }

        /// <summary>
        /// Helper method to safely get long properties from OneAuth AuthResult
        /// </summary>
        // private long GetAuthResultPropertyAsLong(AuthResult authResult, string propertyName)
        // {
        //     try
        //     {
        //         var property = authResult.GetType().GetProperty(propertyName);
        //         var value = property?.GetValue(authResult);
        //         if (value != null && long.TryParse(value.ToString(), out long result))
        //         {
        //             return result;
        //         }
        //         return 3600; // Default 1 hour
        //     }
        //     catch
        //     {
        //         return 3600; // Default 1 hour
        //     }
        // }

        /// <summary>
        /// Converts a OneAuth Account to MSAL IAccount
        /// </summary>
        private bool TryConvertOneAuthAccountToMsalAccount(
            Microsoft.Authentication.Client.Account oneAuthAccount,
            string clientId,
            ILoggerAdapter logger,
            out IAccount msalAccount)
        {
            if (oneAuthAccount == null)
            {
                msalAccount = null;
                return false;
            }

            try
            {
                // Validate required properties
                // Note: OneAuth Account uses DisplayName instead of Username
                if (string.IsNullOrEmpty(oneAuthAccount.Id) ||
                    string.IsNullOrEmpty(oneAuthAccount.HomeAccountId) ||
                    string.IsNullOrEmpty(oneAuthAccount.Environment) ||
                    string.IsNullOrEmpty(oneAuthAccount.DisplayName))
                {
                    logger?.Warning($"[OneAuth] Account has missing required properties. " +
                        $"Id: {!string.IsNullOrEmpty(oneAuthAccount.Id)}, " +
                        $"HomeAccountId: {!string.IsNullOrEmpty(oneAuthAccount.HomeAccountId)}, " +
                        $"Environment: {!string.IsNullOrEmpty(oneAuthAccount.Environment)}, " +
                        $"DisplayName: {!string.IsNullOrEmpty(oneAuthAccount.DisplayName)}");
                    
                    msalAccount = null;
                    return false;
                }

                // Create MSAL account with OneAuth account properties
                // OneAuth Account uses DisplayName which maps to MSAL's username parameter
                msalAccount = new Client.Account(
                    oneAuthAccount.HomeAccountId,
                    oneAuthAccount.DisplayName,
                    oneAuthAccount.Environment,
                    null, // wids (not available from OneAuth Account)
                    new Dictionary<string, string>
                    {
                        { clientId, oneAuthAccount.Id }
                    });

                logger?.Verbose(() => $"[OneAuth] Successfully converted account: {oneAuthAccount.DisplayName}");
                return true;
            }
            catch (Exception ex)
            {
                logger?.Error($"[OneAuth] Failed to convert OneAuth account to MSAL account: {ex}");
                msalAccount = null;
                return false;
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
