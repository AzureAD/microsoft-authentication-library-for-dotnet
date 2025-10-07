using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;
using System;
using System.Threading.Tasks;

namespace OneAuthBrokerSample
{
    /// <summary>
    /// Sample application demonstrating OneAuth broker integration with MSAL.NET
    /// </summary>
    class Program
    {
        private static readonly string ClientId = "1d18b3b0-251b-4714-a02a-9956cec86c2d"; // Microsoft Graph PowerShell
        private static readonly string[] Scopes = { "https://graph.microsoft.com/User.Read" };
        private static readonly string Authority = "https://login.microsoftonline.com/common";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== OneAuth Broker Sample Application ===");
            Console.WriteLine();

            try
            {
                // Create the public client application with OneAuth broker support
                var app = PublicClientApplicationBuilder.Create(ClientId)
                    .WithAuthority(Authority)
                    .WithRedirectUri("http://localhost")
                    .WithWindowsDesktopFeatures(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                    .Build();

                Console.WriteLine("Successfully created PublicClientApplication with OneAuth broker support");
                Console.WriteLine();

                // Run basic tests
                // await RunBasicTestsAsync(app).ConfigureAwait(false);

                // Test OneAuth SignInInteractively specifically
                await TestOneAuthSignInInteractivelyAsync(app).ConfigureAwait(false);

                // Run advanced tests
                // var advancedTests = new OneAuthBrokerTests(app);
                // await RunAdvancedTestsAsync(advancedTests).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Run basic OneAuth broker tests
        /// </summary>
        private static async Task RunBasicTestsAsync(IPublicClientApplication app)
        {
            Console.WriteLine("=== Running Basic OneAuth Broker Tests ===");

            try
            {
                // Test 1: Check for cached accounts
                Console.WriteLine("1. Checking for cached accounts...");
                var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                Console.WriteLine($"   Found {accounts.Count()} cached accounts");

                // Test 2: Attempt silent authentication if accounts exist
                if (accounts.Any())
                {
                    Console.WriteLine("2. Attempting silent authentication...");
                    try
                    {
                        var result = await app.AcquireTokenSilent(Scopes, accounts.First())
                            .ExecuteAsync().ConfigureAwait(false);
                        
                        Console.WriteLine($"   Silent auth successful!");
                        Console.WriteLine($"   User: {result.Account.Username}");
                        Console.WriteLine($"   Token source: {result.AuthenticationResultMetadata.TokenSource}");
                        return; // Success, no need for interactive auth
                    }
                    catch (MsalUiRequiredException)
                    {
                        Console.WriteLine("   Silent auth failed - UI required");
                    }
                }

                // Test 3: Interactive authentication
                Console.WriteLine("3. Attempting interactive authentication...");
                var interactiveResult = await app.AcquireTokenInteractive(Scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .WithParentActivityOrWindow(IntPtr.Zero)
                    .ExecuteAsync().ConfigureAwait(false);

                Console.WriteLine($"   Interactive auth successful!");
                Console.WriteLine($"   User: {interactiveResult.Account.Username}");
                Console.WriteLine($"   Token source: {interactiveResult.AuthenticationResultMetadata.TokenSource}");
                Console.WriteLine($"   Expires: {interactiveResult.ExpiresOn}");
                
                // Display some additional info
                Console.WriteLine($"   Tenant ID: {interactiveResult.TenantId}");
                Console.WriteLine($"   Scopes: {string.Join(", ", interactiveResult.Scopes)}");
            }
            catch (MsalException ex)
            {
                Console.WriteLine($"MSAL error during basic testing: {ex.ErrorCode} - {ex.Message}");
                
                if (ex.ErrorCode == MsalError.BrokerApplicationRequired)
                {
                    Console.WriteLine("OneAuth broker is not available or not installed");
                    Console.WriteLine("This is expected with the current placeholder implementation");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during basic testing: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Test OneAuth broker's SignInInteractively method through MSAL's broker integration
        /// When OneAuth broker is enabled, MSAL's AcquireTokenInteractive internally calls OneAuth's SignInInteractively
        /// </summary>
        private static async Task TestOneAuthSignInInteractivelyAsync(IPublicClientApplication app)
        {
            Console.WriteLine("=== Testing OneAuth Broker SignInInteractively Integration ===");

            try
            {
                Console.WriteLine("Testing MSAL -> OneAuth broker flow:");
                Console.WriteLine("1. MSAL.AcquireTokenInteractive() called");
                Console.WriteLine("2. MSAL detects OneAuth broker is enabled");
                Console.WriteLine("3. MSAL routes to OneAuth.SignInInteractively() internally");
                Console.WriteLine("4. OneAuth handles the authentication flow");
                Console.WriteLine();

                // Call AcquireTokenInteractive - MSAL will internally call OneAuth's SignInInteractively method
                Console.WriteLine("Calling MSAL AcquireTokenInteractive (routes to OneAuth.SignInInteractively)...");
                
                var result = await app.AcquireTokenInteractive(Scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .WithParentActivityOrWindow(IntPtr.Zero)
                    .WithLoginHint("oneauth-test@microsoft.com") // Provide hint for OneAuth
                    .WithExtraQueryParameters("test_oneauth_signin=true") // Signal this is a test
                    .ExecuteAsync().ConfigureAwait(false);

                if (result != null)
                {
                    Console.WriteLine("✅ OneAuth broker SignInInteractively flow succeeded!");
                    Console.WriteLine("   This confirms MSAL successfully routed to OneAuth.SignInInteractively()");
                    Console.WriteLine();
                    Console.WriteLine("   Authentication Result:");
                    Console.WriteLine($"   User: {result.Account?.Username ?? "Unknown"}");
                    Console.WriteLine($"   Tenant: {result.TenantId ?? "Unknown"}");
                    Console.WriteLine($"   Token Source: {result.AuthenticationResultMetadata?.TokenSource ?? Microsoft.Identity.Client.TokenSource.IdentityProvider}");
                    Console.WriteLine($"   Scopes: {string.Join(", ", result.Scopes ?? new string[0])}");
                    Console.WriteLine($"   Expires: {result.ExpiresOn}");
                    Console.WriteLine($"   Correlation ID: {result.CorrelationId}");
                    
                    // Log token info (first few characters only for security)
                    if (!string.IsNullOrEmpty(result.AccessToken))
                    {
                        Console.WriteLine($"   Access Token Length: {result.AccessToken.Length} chars");
                        Console.WriteLine($"   Access Token Preview: {result.AccessToken.Substring(0, Math.Min(10, result.AccessToken.Length))}...");
                    }
                    
                    if (!string.IsNullOrEmpty(result.IdToken))
                    {
                        Console.WriteLine($"   ID Token Length: {result.IdToken.Length} chars");
                    }
                }
                else
                {
                    Console.WriteLine("❌ OneAuth broker flow returned null - check OneAuth.SignInInteractively implementation");
                }
            }
            catch (MsalException ex)
            {
                Console.WriteLine($"❌ MSAL Exception during OneAuth broker test:");
                Console.WriteLine($"   Error Code: {ex.ErrorCode}");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine();
                
                if (ex.ErrorCode == MsalError.BrokerApplicationRequired)
                {
                    Console.WriteLine("   Root Cause: OneAuth broker is not available/installed");
                    Console.WriteLine("   This means MSAL cannot route to OneAuth.SignInInteractively()");
                }
                else if (ex.ErrorCode == MsalError.UnknownBrokerError)
                {
                    Console.WriteLine("   Root Cause: Error in OneAuth broker implementation");
                    Console.WriteLine("   Check the OneAuthBroker.SignInInteractivelyAsync method in OneAuthBroker.cs");
                }
                else if (ex.ErrorCode == MsalError.AuthenticationCanceledError)
                {
                    Console.WriteLine("   Root Cause: User cancelled the authentication dialog");
                    Console.WriteLine("   OneAuth.SignInInteractively was called but user cancelled");
                }
                else
                {
                    Console.WriteLine("   This may indicate an issue in the MSAL -> OneAuth broker routing");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected exception during OneAuth broker test:");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   This suggests an issue in the OneAuth broker integration layer");
                Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Run advanced OneAuth broker tests
        /// </summary>
        private static async Task RunAdvancedTestsAsync(OneAuthBrokerTests tests)
        {
            Console.WriteLine("=== Running Advanced OneAuth Broker Tests ===");

            try
            {
                await tests.TestBrokerAvailabilityAsync().ConfigureAwait(false);
                Console.WriteLine();

                await tests.TestAccountEnumerationAsync().ConfigureAwait(false);
                Console.WriteLine();

                await tests.TestSilentAuthenticationAsync(Scopes).ConfigureAwait(false);
                Console.WriteLine();

                // Test the new SignInInteractively implementation
                await tests.TestSignInInteractivelyAsync(Scopes).ConfigureAwait(false);
                Console.WriteLine();

                // Test SignInInteractively variations
                await tests.TestSignInInteractivelyVariationsAsync().ConfigureAwait(false);
                Console.WriteLine();

                await tests.TestErrorScenariosAsync().ConfigureAwait(false);
                Console.WriteLine();
            }
            catch (MsalException ex)
            {
                Console.WriteLine($"MSAL error during advanced testing: {ex.ErrorCode} - {ex.Message}");
                
                if (ex.ErrorCode == MsalError.BrokerApplicationRequired)
                {
                    Console.WriteLine("OneAuth broker is not available or not installed");
                    Console.WriteLine("This is expected with the current placeholder implementation");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during OneAuth broker testing: {ex.Message}");
            }
        }
    }
}