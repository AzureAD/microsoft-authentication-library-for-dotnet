// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

class ConsoleMacAppWithBroker
{
    private static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;

    private static readonly ConcurrentQueue<(Action Action, TaskCompletionSource<bool> Completion, bool IsAsyncAction)> MainThreadActions = 
        new ConcurrentQueue<(Action, TaskCompletionSource<bool>, bool)>();
    
    private static volatile bool _workerFinished = false;
    
    static async Task Main(string[] args)
    {

        _ = Task.Run(() => BackgroundWorker());

        while (!_workerFinished)
        {
            while (MainThreadActions.TryDequeue(out var actionItem))
            {
                try
                {
                    actionItem.Action();
                    if (!actionItem.IsAsyncAction)
                    {
                        actionItem.Completion.TrySetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    actionItem.Completion.TrySetException(ex);
                }
            }

            Thread.Sleep(10);
        }
        
        Console.WriteLine("Background worker completed. Application exiting.");
    }

    public static Task RunOnMainThreadAsync(Func<Task> asyncAction)
    {
        var tcs = new TaskCompletionSource<bool>();
        Action wrapper = async () => 
        {
            try 
            {
                await asyncAction().ConfigureAwait(false);
                tcs.TrySetResult(true);
            }
            catch (Exception ex) 
            {
                tcs.TrySetException(ex);
            }
        };
        MainThreadActions.Enqueue((wrapper, tcs, true));
        return tcs.Task;
    }

    private static string TruncateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return string.Empty;
            
        return token.Length <= 50 ? token : token.Substring(0, 50) + "...";
    }

    private static async Task SwitchToBackgroundThreadViaHttpRequest()
    {
        Console.WriteLine($"Current thread ID before HTTP request: {Thread.CurrentThread.ManagedThreadId}");
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Make a simple HTTP request to switch context
                HttpResponseMessage response = await client.GetAsync("https://httpbin.org/get").ConfigureAwait(false);
                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine("HTTP request completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP request failed: {ex.Message}");
            }
        }
        
        Console.WriteLine($"Current thread ID (after HTTP request): {Thread.CurrentThread.ManagedThreadId}");
    }

    private static async Task BackgroundWorker()
    {
        try
        {
            PublicClientApplicationBuilder builder = PublicClientApplicationBuilder
                .Create("04b07795-8ddb-461a-bbee-02f9e1bf7b46") // Azure CLI client id
                .WithRedirectUri("msauth.com.msauth.unsignedapp://auth")  // Unsigned app redirect, required by broker team.
                .WithAuthority("https://login.microsoftonline.com/organizations");

            builder = builder.WithLogging(SampleLogging);

            builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.OSX)
            {
                ListOperatingSystemAccounts = false,
                MsaPassthrough = false,
                Title = "MSAL Dev App .NET FX"
            }
            );

            IPublicClientApplication pca = builder.Build();

            AcquireTokenInteractiveParameterBuilder interactiveBuilder = pca.AcquireTokenInteractive(new string[] { "https://graph.microsoft.com/.default" });

            AuthenticationResult result = null;

            // Acquire token interactively on main thread
            await RunOnMainThreadAsync(async () =>
            {
                try
                {
                    // Execute the authentication request on the main thread
                    result = await interactiveBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                    Console.WriteLine("Interactive authentication completed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Interactive authentication error: {ex}");
                    throw;
                }
            }).ConfigureAwait(false);


            Console.WriteLine($"Interactive call. Access token: {TruncateToken(result.AccessToken)}");
            Console.WriteLine($"Expires on: {result.ExpiresOn}");
            
            // Make an HTTP request to switch to a background thread
            await SwitchToBackgroundThreadViaHttpRequest().ConfigureAwait(false);
            
            IAccount account = result.Account;
            AcquireTokenSilentParameterBuilder silentBuilder = pca.AcquireTokenSilent(new string[] { "https://graph.microsoft.com/.default" }, account);

            // Silent call on main thread
            await RunOnMainThreadAsync(async () =>
            {
                try
                {
                    // Execute the silent authentication request on the main thread
                    result = await silentBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                    Console.WriteLine("Second interactive authentication completed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Second interactive authentication error: {ex}");
                    throw;
                }
            }).ConfigureAwait(false);

            Console.WriteLine($"Silent Call. Access token: {TruncateToken(result.AccessToken)}");
            Console.WriteLine($"Expires on: {result.ExpiresOn}");

            // Make an HTTP request to switch to a background thread
            await SwitchToBackgroundThreadViaHttpRequest().ConfigureAwait(false);

            // Second interactive call on main thread
            await RunOnMainThreadAsync(async () =>
            {
                try
                {
                    // Execute the authentication request on the main thread
                    result = await interactiveBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                    Console.WriteLine("Second interactive authentication completed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Second interactive authentication error: {ex}");
                    throw;
                }
            }).ConfigureAwait(false);

            Console.WriteLine($"Second interactive call. Access token: {TruncateToken(result.AccessToken)}");
            Console.WriteLine($"Expires on: {result.ExpiresOn}");

            // Make an HTTP request to switch to a background thread
            await SwitchToBackgroundThreadViaHttpRequest().ConfigureAwait(false);

            try
            {
                // Execute the authentication request on the main thread
                result = await interactiveBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine("Third interactive call should not succeed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Third interactive authentication error: {ex}");

                Console.WriteLine($"\nNotice! The third interactive call fails with status ApiContractViolation is expected. The interactive call should happen in the main thread.\n");
            }
        }
        finally
        {
            // Signal that the worker has finished, regardless of success or failure
            _workerFinished = true;
        }
    }

	private static void SampleLogging(LogLevel level, string message, bool containsPii)
	{
		try
        {
			string homeDirectory = Environment.GetEnvironmentVariable("HOME");
            string filePath = Path.Combine(homeDirectory, "msalnet.log");
			using (StreamWriter writer = new StreamWriter(filePath, append: true))
			{
				writer.WriteLine($"{level} {message}");
			}
		}
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write log: {ex.Message}");
        }
	}

}


