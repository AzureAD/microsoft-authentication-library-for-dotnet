// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

class ConsoleMacAppWithBroker
{
    private static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;
    
    private static readonly ConcurrentQueue<(Action Action, TaskCompletionSource<bool> Completion, bool IsAsyncAction)> MainThreadActions = 
        new ConcurrentQueue<(Action, TaskCompletionSource<bool>, bool)>();
    static async Task Main(string[] args)
    {
        
        _ = Task.Run(() => BackgroundWorker());

        while (true)
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
    
    private static async Task BackgroundWorker()
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


        Console.WriteLine($"Interactive call. Access token: {result.AccessToken}");
        Console.WriteLine($"Expires on: {result.ExpiresOn}");

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

        Console.WriteLine($"Silent Call. Access token: {result.AccessToken}");
        Console.WriteLine($"Expires on: {result.ExpiresOn}");

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

        Console.WriteLine($"Second interactive call. Access token: {result.AccessToken}");
        Console.WriteLine($"Expires on: {result.ExpiresOn}");
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


