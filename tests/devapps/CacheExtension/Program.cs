// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace ManualTestApp
{
    /// <summary>
    /// This advanced console app uses MSAL with the token cache based on Config.cs to show how various MSAL flows work.
    /// If you are new to this sample, please look at Config and ExampleUsage 
    /// </summary>
    class Program
    {
#pragma warning disable UseAsyncSuffix // Use Async suffix
        static async Task Main(string[] args)
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {

            // It's recommended to create a separate PublicClient Application for each tenant
            // but only one CacheHelper object
            var pca = CreatePublicClient("https://login.microsoftonline.com/organizations");
            var cacheHelper = await CreateCacheHelperAsync().ConfigureAwait(false);
            cacheHelper.RegisterCache(pca.UserTokenCache);

            // Advanced scenario for when 2 or more apps share the same cache             
            cacheHelper.CacheChanged += (_, e) => // this event is very expensive perf wise
            {
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"Cache Changed, Added: {e.AccountsAdded.Count()} Removed: {e.AccountsRemoved.Count()}");
                Console.ResetColor();
            };

            AuthenticationResult result;

            while (true)
            {
                // Display menu
                Console.WriteLine($@"
                        1. Acquire Token using Username and Password - for TEST only, do not use in production!
                        2. Acquire Token using Device Code Flow
                        3. Acquire Token Interactive
                        4. Acquire Token Silent
                        5. Display Accounts (reads the cache)
                        6. Acquire Token U/P and Silent in a loop                        
                        7. Use persistence layer to read / write any data
                        8. Use persistence layer to read / write any data with process-level lock
                        c. Clear cache
                        e. Expire Access Tokens (TEST only!)
                        x. Exit app
                    Enter your selection: ");
                char.TryParse(Console.ReadLine(), out var selection);
                try
                {
                    switch (selection)
                    {
                        case '1': //  Acquire Token using Username and Password (requires config)

                            // IMPORTANT: you should ALWAYS try to get a token silently first

                            result = await AcquireTokenROPCAsync(pca).ConfigureAwait(false);
                            DisplayResult(result);

                            break;

                        case '2': // Device Code Flow
                                  // IMPORTANT: you should ALWAYS try to get a token silently first

                            result = await pca.AcquireTokenWithDeviceCode(Config.Scopes, (dcr) =>
                            {
                                Console.BackgroundColor = ConsoleColor.DarkCyan;
                                Console.WriteLine(dcr.Message);
                                Console.ResetColor();

                                return Task.FromResult(1);
                            }).ExecuteAsync().ConfigureAwait(false);
                            DisplayResult(result);

                            break;
                        case '3': // Interactive
                                  // IMPORTANT: you should ALWAYS try to get a token silently first

                            result = await pca.AcquireTokenInteractive(Config.Scopes)
                                .ExecuteAsync()
                                .ConfigureAwait(false);
                            DisplayResult(result);
                            break;
                        case '4': // Silent


                            Console.WriteLine("Getting all the accounts. This reads the cache");
                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                            var firstAccount = accounts.FirstOrDefault();

                            // this is expected to fail when account is null
                            result = await pca.AcquireTokenSilent(Config.Scopes, firstAccount)
                                .ExecuteAsync()
                                .ConfigureAwait(false);
                            DisplayResult(result);
                            break;
                        case '5': // Display Accounts
                            Console.Clear();
                            var accounts2 = await pca.GetAccountsAsync().ConfigureAwait(false);
                            if (!accounts2.Any())
                            {
                                Console.WriteLine("No accounts were found in the cache.");
                            }

                            foreach (var acc in accounts2)
                            {
                                Console.WriteLine($"Account for {acc.Username}");
                            }
                            break;
                        case '6': // U/P and Silent in a loop
                            Console.WriteLine("CTRL-C to stop...");

#pragma warning disable CS0618 // Type or member is obsolete
                            cacheHelper.Clear();
#pragma warning restore CS0618 // Type or member is obsolete


                            var pca2 = CreatePublicClient("https://login.microsoftonline.com/organizations");
                            var pca3 = CreatePublicClient("https://login.microsoftonline.com/organizations");
                            cacheHelper.RegisterCache(pca2.UserTokenCache);
                            cacheHelper.RegisterCache(pca3.UserTokenCache);

                            while (true)
                            {
                                await Task.WhenAll(
                                    RunRopcAndSilentAsync("PCA_1", pca),
                                    RunRopcAndSilentAsync("PCA_2", pca2),
                                    RunRopcAndSilentAsync("PCA_3", pca3)

                                ).ConfigureAwait(false);

                                Trace.Flush();
                                await Task.Delay(2000).ConfigureAwait(false);
                            }

                        case '7':

                            var storageProperties = new StorageCreationPropertiesBuilder(
                               Config.CacheFileName + ".other_secrets",
                               Config.CacheDir)
                            .WithMacKeyChain(
                               Config.KeyChainServiceName + ".other_secrets",
                               Config.KeyChainAccountName);

                            Storage storage = Storage.Create(storageProperties.Build());
                            //string lockFilePath = Path.Combine(Config.CacheDir, Config.CacheFileName + ".other_secrets.lockfile");

                            byte[] secretBytes = Encoding.UTF8.GetBytes("secret");
                            Console.WriteLine("Writing...");
                            storage.WriteData(secretBytes);

                            Console.WriteLine("Writing again...");
                            storage.WriteData(secretBytes);


                            Console.WriteLine("Reading...");
                            var data = storage.ReadData();
                            Console.WriteLine("Read: " + Encoding.UTF8.GetString(data));

                            Console.WriteLine("Deleting...");
                            storage.Clear();


                            break;


                        case '8':

                            storageProperties = new StorageCreationPropertiesBuilder(
                                Config.CacheFileName + ".other_secrets2",
                                Config.CacheDir)
                                .WithMacKeyChain(
                           Config.KeyChainServiceName + ".other_secrets2",
                           Config.KeyChainAccountName);

                            storage = Storage.Create(storageProperties.Build());

                            string lockFilePath = Path.Combine(Config.CacheDir, Config.CacheFileName + ".lockfile");

                            using (new CrossPlatLock(lockFilePath)) // cross-process only
                            {
                                secretBytes = Encoding.UTF8.GetBytes("secret");
                                Console.WriteLine("Writing...");
                                storage.WriteData(secretBytes);

                                Console.WriteLine("Writing again...");
                                storage.WriteData(secretBytes);

                                // if another process (not thread!) attempts to read / write this secret
                                // and uses the CrossPlatLock mechanism, it will wait for the lock to be released first
                                await Task.Delay(1000).ConfigureAwait(false);

                                Console.WriteLine("Reading...");
                                data = storage.ReadData();
                                Console.WriteLine("Read: " + Encoding.UTF8.GetString(data));

                                Console.WriteLine("Deleting...");
                                storage.Clear();
                            } // lock released

                            break;

                        case 'c':
                            var accounts4 = await pca.GetAccountsAsync().ConfigureAwait(false);
                            foreach (var acc in accounts4)
                            {
                                Console.WriteLine($"Removing account for {acc.Username}");
                                await pca.RemoveAsync(acc).ConfigureAwait(false);
                            }
                            Console.Clear();

                            break;

                        case 'e': // This is only meant for testing purposes

                            // do smth that loads the cache first
                            await pca.GetAccountsAsync().ConfigureAwait(false);

                            DateTimeOffset expiredValue = DateTimeOffset.UtcNow.AddMonths(-1);

                            var accessor = pca.UserTokenCache.GetType()
                                .GetRuntimeProperties()
                                .Single(p => p.Name == "Microsoft.Identity.Client.ITokenCacheInternal.Accessor")
                                .GetValue(pca.UserTokenCache);

                            var internalAccessTokens = accessor.GetType().GetMethod("GetAllAccessTokens").Invoke(accessor, new object[] { null }) as IEnumerable<object>;

                            foreach (var internalAt in internalAccessTokens)
                            {
                                internalAt.GetType().GetRuntimeMethods().Single(m => m.Name == "set_ExpiresOn").Invoke(internalAt, new object[] { expiredValue });
                                accessor.GetType().GetMethod("SaveAccessToken").Invoke(accessor, new[] { internalAt });
                            }

                            var ctor = typeof(TokenCacheNotificationArgs).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

                            var notificationArgs = ctor.Invoke(new object[] { pca.UserTokenCache, Config.ClientId, null, true, false, true, null, null, null });
                            var task = pca.UserTokenCache.GetType().GetRuntimeMethods()
                                .Single(m => m.Name == "Microsoft.Identity.Client.ITokenCacheInternal.OnAfterAccessAsync")
                                .Invoke(pca.UserTokenCache, new[] { notificationArgs });

                            await (task as Task).ConfigureAwait(false);
                            break;

                        case 'x':
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Exception : " + ex);
                    Console.ResetColor();
                    Console.WriteLine("Hit Enter to continue");

                    Console.Read();
                }
            }
        }

        private static async Task<AuthenticationResult> RunRopcAndSilentAsync(
            string logPrefix,
            IPublicClientApplication pca)
        {
            Console.WriteLine($"{logPrefix} Acquiring token by ROPC...");
            var result = await AcquireTokenROPCAsync(pca).ConfigureAwait(false);

            Console.WriteLine($"{logPrefix} OK. Now getting the accounts");
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Console.WriteLine($"{logPrefix} Acquiring token silent");

            result = await pca.AcquireTokenSilent(Config.Scopes, accounts.First())
                .ExecuteAsync()
                .ConfigureAwait(false);

            Console.WriteLine($"{logPrefix} Deleting the account");
            foreach (var acc in accounts)
            {
                await pca.RemoveAsync(acc).ConfigureAwait(false);
            }

            return result;
        }

        private static async Task<AuthenticationResult> AcquireTokenROPCAsync(
            IPublicClientApplication pca)
        {
            if (string.IsNullOrEmpty(Config.Username) ||
                string.IsNullOrEmpty(Config.Password))
            {
                throw new InvalidOperationException("Please configure a username and password!");
            }

            return await pca.AcquireTokenByUsernamePassword(
                Config.Scopes,
                Config.Username,
                Config.Password)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        private static void DisplayResult(AuthenticationResult result)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Token Acquisition Success! Got a token for: " + result.Account.Username);
            Console.WriteLine("Token source:" + result.AuthenticationResultMetadata.TokenSource);
            Console.WriteLine(result.AccessToken);
            Console.ResetColor();
            Console.WriteLine("Press ENTER to continue");
            Console.Read();
            Console.Clear();
        }

        private static async Task<MsalCacheHelper> CreateCacheHelperAsync()
        {
            StorageCreationProperties storageProperties;

            try
            {
                storageProperties = new StorageCreationPropertiesBuilder(
                    Config.CacheFileName,
                    Config.CacheDir)
                .WithLinuxKeyring(
                    Config.LinuxKeyRingSchema,
                    Config.LinuxKeyRingCollection,
                    Config.LinuxKeyRingLabel,
                    Config.LinuxKeyRingAttr1,
                    Config.LinuxKeyRingAttr2)
                .WithMacKeyChain(
                    Config.KeyChainServiceName,
                    Config.KeyChainAccountName)
                .WithCacheChangedEvent( // do NOT use unless really necessary, high perf penalty!
                    Config.ClientId,
                    Config.Authority)
                .Build();

                var cacheHelper = await MsalCacheHelper.CreateAsync(
                    storageProperties).ConfigureAwait(false);

                cacheHelper.VerifyPersistence();
                return cacheHelper;

            }
            catch (MsalCachePersistenceException e)
            {
                Console.WriteLine($"WARNING! Unable to encrypt tokens at rest." +
                    $" Saving tokens in plaintext at {Path.Combine(Config.CacheDir, Config.CacheFileName)} ! Please protect this directory or delete the file after use");
                Console.WriteLine($"Encryption exception: " + e);

                storageProperties =
                    new StorageCreationPropertiesBuilder(
                        Config.CacheFileName + ".plaintext", // do not use the same file name so as not to overwrite the encrypted version
                        Config.CacheDir)
                    .WithUnprotectedFile()
                    .Build();

                var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties).ConfigureAwait(false);
                cacheHelper.VerifyPersistence();

                return cacheHelper;
            }
        }

        private static IPublicClientApplication CreatePublicClient(string authority)
        {
            var appBuilder = PublicClientApplicationBuilder.Create(Config.ClientId) // DO NOT USE THIS CLIENT ID IN YOUR APP!!!! WE WILL DELETE IT!
                .WithAuthority(authority)
                .WithRedirectUri("http://localhost"); // make sure to register this redirect URI for the interactive login to work

            var app = appBuilder.Build();
            Console.WriteLine($"Built public client");

            return app;
        }

    }

}
