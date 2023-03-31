// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

#if NET48 // This for classic net
using Microsoft.Identity.Client.Desktop;
#elif !NET6_WIN && NET6_0 // this is for pure net6.0 and not net6.0-windows10.0.17763.0
using Microsoft.Identity.Client.Broker;
#endif

namespace TestBrokerApp
{
    internal class PCATester
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        #region Variable
        // This app has http://localhost redirect uri registered
        private static readonly string s_clientIdForPublicApp = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
        internal static readonly IEnumerable<string> Scopes = new[] { "user.read", "openid" };
        #endregion

        internal const string AuthorityCommon = "https://login.microsoftonline.com/common";
        internal const string AuthorityConsumers = "https://login.microsoftonline.com/consumers";
        internal const string AuthorityOrganizations = "https://login.microsoftonline.com/organizations";
        internal const string AuthorityTenant = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47";

        internal string Authority { get; set; } = AuthorityCommon;
        internal bool HasMsaPasThrough { get; set; }

        internal bool ListOSAccounts { get; set; }

        internal PublicClientApplicationBuilder CreatePcaBuilder()
        {
            var pcaBuilder = CreatePcaBuilderNoBroker();

            BrokerOptions options = new BrokerOptions(BrokerOptions.OperatingSystems.Windows);
            options.Title = "new Runtime broker";
            options.ListOperatingSystemAccounts = ListOSAccounts;
            options.MsaPassthrough = HasMsaPasThrough;

            // here is the framework specific code for broker
#if NET48 // This for classic net
            pcaBuilder.WithWindowsDesktopFeatures(options);
#elif !NET6_WIN && NET6_0 // this is for pure net6.0 and not net6.0-windows10.0.17763.0
            pcaBuilder.WithBroker(options);
#else 
            // this is for net-win (Note: Not setting options)
            // for other platforms i.e. UWP and MAUI. There are seperate projects.
            pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows) { Title = "Only Windows" });
            // pcaBuilder.WithBroker(); // Only WithBroker with no options settings.
#endif

            return pcaBuilder;
        }

        internal PublicClientApplicationBuilder CreatePcaBuilderNoBroker()
        {

            IntPtr consoleWindowHandle = GetConsoleWindow();
            Func<IntPtr> consoleWindowHandleProvider = () => consoleWindowHandle;

            var pcaBuilder = PublicClientApplicationBuilder
                            .Create(s_clientIdForPublicApp)
                            .WithAuthority(Authority)
                            .WithLogging(Log, LogLevel.Verbose, true)
                            .WithRedirectUri("http://localhost") // required for DefaultOsBrowser
                            .WithParentActivityOrWindow(consoleWindowHandleProvider);

            return pcaBuilder;
        }

        internal async Task<AuthenticationResult> ExecuteAndDisplay(PublicClientApplicationBuilder pcaBuilder)
        {
            try
            {
                var pca = pcaBuilder.Build();

                AcquireTokenInteractiveParameterBuilder atiParamBuilder = pca.AcquireTokenInteractive(PCATester.Scopes);

                return await ExecuteAndDisplay(atiParamBuilder).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"----------------- Exception -----------------");
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }

            return null;
        }

        internal async Task<AuthenticationResult> ExecuteAndDisplay<T>(T atiParamBuilder) where T : BaseAbstractAcquireTokenParameterBuilder<T>
        {
            try
            {
                AuthenticationResult result = await atiParamBuilder.ExecuteAsync().ConfigureAwait(false);
                if (result.AccessToken != null)
                {
                    Console.WriteLine("\r\n");
                    Console.WriteLine("\r\n");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("+++😁😁😁 Success - Got Token 😁😁😁+++");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("---😿😿😿 Fail 😿😿😿---");
                }

                Console.ResetColor();
                return result;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"----------------- Exception -----------------");
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }

            return null;
        }


        private static void Log(LogLevel level, string message, bool containsPii)
        {
            if (!containsPii)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
            }

            switch (level)
            {
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case LogLevel.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                default:
                    break;
            }

            Console.WriteLine($"{level} {message}");
            Console.ResetColor();
        }

    }
}
