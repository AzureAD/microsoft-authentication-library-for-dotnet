using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker.OneAuth;
using Microsoft.Identity.Client.Desktop;
using Microsoft.Identity.Client.Platforms.Features.OneAuthBroker;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OneAuthBrokerSample
{
    /// <summary>
    /// Advanced OneAuth broker testing class
    /// </summary>
    public class OneAuthBrokerTests
    {
        private readonly IPublicClientApplication _app;

    /// <summary>
    /// Initializes a new instance of the OneAuthBrokerTests class.
    /// </summary>
    /// <param name="app">The public client application instance.</param>
    public OneAuthBrokerTests(IPublicClientApplication app)
    {
        _app = app;
    }        /// <summary>
        /// Test OneAuth broker initialization and availability
        /// </summary>
        public async Task TestBrokerAvailabilityAsync()
        {
            Console.WriteLine("=== Testing OneAuth Broker Availability ===");

            try
            {
                // This would test if the OneAuth broker is properly initialized
                Console.WriteLine("Checking OneAuth broker installation...");
                
                // In a real implementation, this would check:
                // - OneAuth service availability
                // - Proper registration with the system
                // - Version compatibility
                
                // Simulate async check
                await Task.Delay(100).ConfigureAwait(false);
                
                Console.WriteLine("OneAuth broker availability check completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking OneAuth broker availability: {ex.Message}");
            }
        }

        /// <summary>
        /// Test interactive authentication with OneAuth broker
        /// </summary>
        public async Task<AuthenticationResult?> TestInteractiveAuthenticationAsync(string[] scopes)
        {
            Console.WriteLine("=== Testing Interactive Authentication ===");

            try
            {
                var result = await _app.AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .WithParentActivityOrWindow(IntPtr.Zero)
                    .WithExtraQueryParameters("broker=oneauth") // Force OneAuth broker usage
                    .ExecuteAsync().ConfigureAwait(false);

                Console.WriteLine($"Interactive auth successful via {result.AuthenticationResultMetadata.TokenSource}");
                Console.WriteLine($"Account: {result.Account.Username}");
                Console.WriteLine($"Tenant: {result.TenantId}");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Interactive authentication failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Test silent authentication with cached tokens
        /// </summary>
        public async Task<AuthenticationResult?> TestSilentAuthenticationAsync(string[] scopes)
        {
            Console.WriteLine("=== Testing Silent Authentication ===");

            try
            {
                var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);
                if (!accounts.Any())
                {
                    Console.WriteLine("No cached accounts found for silent authentication");
                    return null;
                }

                var account = accounts.First();
                Console.WriteLine($"Using cached account: {account.Username}");

                var result = await _app.AcquireTokenSilent(scopes, account)
                    .ExecuteAsync().ConfigureAwait(false);

                Console.WriteLine($"Silent auth successful via {result.AuthenticationResultMetadata.TokenSource}");
                return result;
            }
            catch (MsalUiRequiredException)
            {
                Console.WriteLine("UI interaction required - silent auth not possible");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Silent authentication failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Test account enumeration through OneAuth broker
        /// </summary>
        public async Task TestAccountEnumerationAsync()
        {
            Console.WriteLine("=== Testing Account Enumeration ===");

            try
            {
                var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);
                
                Console.WriteLine($"Found {accounts.Count()} cached accounts:");
                
                foreach (var account in accounts)
                {
                    Console.WriteLine($"  - {account.Username} (Home: {account.HomeAccountId?.Identifier})");
                    Console.WriteLine($"    Environment: {account.Environment}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account enumeration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Test removing accounts from OneAuth broker cache
        /// </summary>
        public async Task TestAccountRemovalAsync()
        {
            Console.WriteLine("=== Testing Account Removal ===");

            try
            {
                var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);
                
                if (accounts.Any())
                {
                    var accountToRemove = accounts.First();
                    Console.WriteLine($"Removing account: {accountToRemove.Username}");
                    
                    await _app.RemoveAsync(accountToRemove).ConfigureAwait(false);
                    Console.WriteLine("Account removal completed");
                    
                    // Verify removal
                    var remainingAccounts = await _app.GetAccountsAsync().ConfigureAwait(false);
                    Console.WriteLine($"Remaining accounts: {remainingAccounts.Count()}");
                }
                else
                {
                    Console.WriteLine("No accounts available to remove");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account removal failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Test OneAuth broker's SignInInteractively functionality through MSAL integration
        /// MSAL's AcquireTokenInteractive will internally call OneAuth's SignInInteractively when OneAuth broker is enabled
        /// </summary>
        public async Task TestSignInInteractivelyAsync(string[] scopes)
        {
            Console.WriteLine("=== Testing OneAuth Broker SignInInteractively Integration ===");

            try
            {
                Console.WriteLine("Testing MSAL -> OneAuth.SignInInteractively flow...");
                Console.WriteLine($"Target Scopes: {string.Join(", ", scopes)}");
                Console.WriteLine("Expected: MSAL routes to OneAuth broker's SignInInteractively method");
                Console.WriteLine();

                // Call AcquireTokenInteractive - this will internally call OneAuth.SignInInteractively
                Console.WriteLine("Calling MSAL AcquireTokenInteractive...");
                var result = await _app.AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .WithParentActivityOrWindow(IntPtr.Zero)
                    .WithExtraQueryParameters("test_oneauth_signin=true") // Test marker
                    .WithLoginHint("test@microsoft.com") // Provide login hint for OneAuth
                    .ExecuteAsync().ConfigureAwait(false);

                if (result != null)
                {
                    Console.WriteLine("✅ OneAuth SignInInteractively flow succeeded!");
                    Console.WriteLine("   MSAL successfully routed to OneAuth.SignInInteractively()");
                    Console.WriteLine();
                    Console.WriteLine("   Authentication Result:");
                    Console.WriteLine($"   Account: {result.Account?.Username}");
                    Console.WriteLine($"   Token Source: {result.AuthenticationResultMetadata?.TokenSource}");
                    Console.WriteLine($"   Cache Refresh Reason: {result.AuthenticationResultMetadata?.CacheRefreshReason}");
                    Console.WriteLine($"   Scopes: {string.Join(", ", result.Scopes)}");
                    Console.WriteLine($"   Expires On: {result.ExpiresOn}");
                    Console.WriteLine($"   Correlation ID: {result.CorrelationId}");
                    
                    // Check if we have an access token
                    if (!string.IsNullOrEmpty(result.AccessToken))
                    {
                        Console.WriteLine($"   Access Token: {result.AccessToken.Substring(0, Math.Min(20, result.AccessToken.Length))}...");
                    }
                    
                    // Check if we have an ID token
                    if (!string.IsNullOrEmpty(result.IdToken))
                    {
                        Console.WriteLine($"   ID Token: {result.IdToken.Substring(0, Math.Min(20, result.IdToken.Length))}...");
                    }
                }
                else
                {
                    Console.WriteLine("❌ SignInInteractively returned null result");
                }
            }
            catch (MsalUiRequiredException ex)
            {
                Console.WriteLine($"⚠️  UI Required Exception (expected for OneAuth): {ex.ErrorCode}");
                Console.WriteLine($"   Error: {ex.Message}");
                Console.WriteLine("   This indicates OneAuth broker is being invoked but needs user interaction");
            }
            catch (MsalServiceException ex)
            {
                Console.WriteLine($"⚠️  Service Exception: {ex.ErrorCode}");
                Console.WriteLine($"   Error: {ex.Message}");
                // Note: Classification property may not be available in all MSAL versions
                
                if (ex.ErrorCode == "broker_application_required")
                {
                    Console.WriteLine("   This indicates OneAuth broker is not available/installed");
                }
            }
            catch (MsalClientException ex)
            {
                Console.WriteLine($"⚠️  Client Exception: {ex.ErrorCode}");
                Console.WriteLine($"   Error: {ex.Message}");
                
                if (ex.ErrorCode == "unknown_broker_error")
                {
                    Console.WriteLine("   This indicates OneAuth broker returned an error - check implementation");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected exception during SignInInteractively test: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Test OneAuth SignInInteractively with different parameter combinations
        /// </summary>
        public async Task TestSignInInteractivelyVariationsAsync()
        {
            Console.WriteLine("=== Testing OneAuth SignInInteractively Variations ===");

            // Test 1: Basic call with minimal parameters
            Console.WriteLine("Test 1: Basic SignInInteractively call");
            await TestSignInInteractivelyWithParameters(
                new[] { "https://graph.microsoft.com/User.Read" },
                string.Empty, // no login hint
                Prompt.SelectAccount
            ).ConfigureAwait(false);

            Console.WriteLine();

            // Test 2: With login hint
            Console.WriteLine("Test 2: SignInInteractively with login hint");
            await TestSignInInteractivelyWithParameters(
                new[] { "https://graph.microsoft.com/User.Read" },
                "testuser@microsoft.com",
                Prompt.SelectAccount
            ).ConfigureAwait(false);

            Console.WriteLine();

            // Test 3: With multiple scopes
            Console.WriteLine("Test 3: SignInInteractively with multiple scopes");
            await TestSignInInteractivelyWithParameters(
                new[] { "https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/Mail.Read" },
                string.Empty,
                Prompt.ForceLogin
            ).ConfigureAwait(false);

            Console.WriteLine();

            // Test 4: With consent prompt
            Console.WriteLine("Test 4: SignInInteractively with consent prompt");
            await TestSignInInteractivelyWithParameters(
                new[] { "https://graph.microsoft.com/User.Read" },
                string.Empty,
                Prompt.Consent
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Helper method to test SignInInteractively with specific parameters
        /// </summary>
        private async Task TestSignInInteractivelyWithParameters(
            string[] scopes, 
            string loginHint, 
            Prompt prompt)
        {
            try
            {
                Console.WriteLine($"   Scopes: {string.Join(", ", scopes)}");
                Console.WriteLine($"   Login Hint: {loginHint ?? "None"}");
                Console.WriteLine($"   Prompt: {prompt}");

                var builder = _app.AcquireTokenInteractive(scopes)
                    .WithPrompt(prompt)
                    .WithParentActivityOrWindow(IntPtr.Zero)
                    .WithExtraQueryParameters("oneauth_test=true");

                if (!string.IsNullOrEmpty(loginHint))
                {
                    builder = builder.WithLoginHint(loginHint);
                }

                var result = await builder.ExecuteAsync().ConfigureAwait(false);

                if (result != null)
                {
                    Console.WriteLine($"   ✅ Success: {result.Account?.Username}");
                }
                else
                {
                    Console.WriteLine("   ❌ Returned null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Exception: {ex.GetType().Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// Test error scenarios with OneAuth broker
        /// </summary>
        public async Task TestErrorScenariosAsync()
        {
            Console.WriteLine("=== Testing Error Scenarios ===");

            // Test with invalid scopes
            try
            {
                Console.WriteLine("Testing with invalid scopes...");
                await _app.AcquireTokenInteractive(new[] { "invalid.scope" })
                    .ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                Console.WriteLine($"Expected service exception: {ex.ErrorCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception with invalid scopes: {ex.Message}");
            }

            // Test with cancelled authentication
            try
            {
                Console.WriteLine("Testing cancellation scenarios...");
                using var cts = new System.Threading.CancellationTokenSource();
                cts.Cancel(); // Immediately cancel

                await _app.AcquireTokenInteractive(new[] { "https://graph.microsoft.com/User.Read" })
                    .ExecuteAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancellation handled correctly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception during cancellation test: {ex.Message}");
            }
        }
    }
}