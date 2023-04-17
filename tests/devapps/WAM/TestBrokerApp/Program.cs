using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace TestBrokerApp
{
    internal class Program
    {


        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            await RunConsoleAppLogicAsync().ConfigureAwait(false);
        }

        private static async Task RunConsoleAppLogicAsync()
        {
            while (true) 
            {
                Console.Clear();
                Console.ResetColor();

                // display menu
                Console.WriteLine($"*** Choose your option*** \r\n" + "" +
                         "\t1. Acquire Token Interactive \r\n" +
                         "\t2. Acquire Token Interactive with login hint \r\n" +
                         "\t3. Acquire Token Interactive With Account \r\n" +
                         "\t4. With Default OS Account AAD \r\n" +
                         "\t5. With Default OS Account MSA \r\n" +
                         "\t6. With Pop \r\n" +
                         "\t7. With MsaPassthrough  \r\n" +
                         "\t8. With ListWorkOrSchoolAccounts \r\n" +
                         "\t9. Fallback to browser \r\n" +
                        "\t10. Silent with no account \r\n" +
                        "\t11. Silent with account \r\n" +
                        "\t12. Silent with login hint \r\n" +
                        "\t13. Authority Org \r\n" +
                        "\t14. Authority Consumers \r\n" +
                        "\t15. Authority Tenant \r\n" +
                        "\t16. GetAccounts - Normal \r\n" +
                        "\t17. GetAccounts - ListWorkOrSchoolAccounts \r\n" +
                        "\t18. RemoveAccount -> fails in silent \r\n" +
                        "\t19. ROPC \r\n" +
                        "\t20. Signout \r\n" +
                        "\t21. Switch Account \r\n" +
                        "\t0. Exit app \r\n" +
                        "Enter your Selection: ");
                int.TryParse(Console.ReadLine(), out var selection);
                switch (selection)
                {
                    case 1: // Acquire Token Interactive
                        {
                            PCATester pcaTester= new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            await pcaTester.ExecuteAndDisplay(pcaBuilder).ConfigureAwait(false);
                        }
                        break;

                    case 2: // Acquire Token Interactive with login hint
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            
                            var pca = pcaBuilder.Build();
                            AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);

                            Console.Write("Please enter email for login hint: ");
                            string email = Console.ReadLine();

                            atiBulider.WithLoginHint(email);

                            await pcaTester.ExecuteAndDisplay(atiBulider).ConfigureAwait(false);
                        }
                        break;

                    case 3: // Acquire Token Interactive with Account
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            var pca = pcaBuilder.Build();
                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                            if (accounts != null && accounts.Any())
                            {
                                AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);

                                atiBulider.WithAccount(accounts.First());

                                await pcaTester.ExecuteAndDisplay(atiBulider).ConfigureAwait(false);
                            }
                            else
                            {
                                Console.WriteLine("\r\n");
                                Console.WriteLine("\r\n");
                                Console.ForegroundColor= ConsoleColor.Red;
                                Console.WriteLine("--- No Accounts Present ---");
                                Console.ResetColor();
                            }
                        }
                        break;

                    case 4: // Acquire Token Interactive with Default OS Account AAD
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            var pca = pcaBuilder.Build();
                            AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);
                            atiBulider.WithAccount(PublicClientApplication.OperatingSystemAccount);
                            await pcaTester.ExecuteAndDisplay(atiBulider).ConfigureAwait(false);
                        }
                        break;


                    case 5: // Acquire Token Interactive with Default OS Account MSA
                        {
                            PCATester pcaTester = new PCATester();
                            pcaTester.HasMsaPasThrough = true;
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            var pca = pcaBuilder.Build();
                            AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);
                            atiBulider.WithAccount(PublicClientApplication.OperatingSystemAccount);
                            await pcaTester.ExecuteAndDisplay(atiBulider).ConfigureAwait(false);
                        }
                        break;

                    case 6: // Pop
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            var pca = pcaBuilder.Build();
                            AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);
                            Uri popUri = new Uri("https://www.contoso.com/path1/path2");
                            atiBulider.WithProofOfPossession("12345", HttpMethod.Get, popUri);

                            var result = await pcaTester.ExecuteAndDisplay(atiBulider).ConfigureAwait(false);
                            if (result.TokenType == "PoP")
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("+++ :) :) :) Got Pop Token :) :) :) +++");
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("--- :( :( :( No Pop Token :( :( :( ---");
                            }

                            Console.ResetColor();
                        }
                        break;

                    case 7: // Acquire Token Interactive with login hint and MSAPassthrough
                        {
                            PCATester pcaTester = new PCATester();
                            pcaTester.HasMsaPasThrough = true;
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();

                            var pca = pcaBuilder.Build();
                            AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);

                            Console.Write("Please enter MSA email for login hint: ");
                            string email = Console.ReadLine();

                            atiBulider.WithLoginHint(email);

                            await pcaTester.ExecuteAndDisplay(atiBulider).ConfigureAwait(false);
                        }
                        break;

                    case 8: // Acquire Token Interactive with ListWorkOrSchool accounts
                        {
                            PCATester pcaTester = new PCATester();
                            pcaTester.ListOSAccounts = true;
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();

                            var pca = pcaBuilder.Build();
                            AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);

                            await pcaTester.ExecuteAndDisplay(atiBulider).ConfigureAwait(false);
                        }
                        break;

                    case 9: // Fallback to Browser
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilderNoBroker();

                            var pca = pcaBuilder.Build();
                            AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);

                            await pcaTester.ExecuteAndDisplay(atiBulider).ConfigureAwait(false);
                        }
                        break;

                    case 10: // Silent with no account
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();

                            var pca = pcaBuilder.Build();
                            AcquireTokenSilentParameterBuilder atsBuilder = pca.AcquireTokenSilent(PCATester.Scopes, (IAccount)null);
                            try
                            {
                                await atsBuilder.ExecuteAsync().ConfigureAwait(false);
                            }
                            catch (MsalUiRequiredException )
                            {
                                Console.WriteLine("\r\n");
                                Console.WriteLine("\r\n");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("+++😁😁😁 Success - threw the desired exception 😁😁😁+++");
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("---😿😿😿 Fail 😿😿😿---");
                                Console.WriteLine(ex.ToString());
                            }
                        }
                        break;

                    case 11: // Silent with account
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();

                            var pca = pcaBuilder.Build();
                            AcquireTokenSilentParameterBuilder atsBuilder = pca.AcquireTokenSilent(PCATester.Scopes, PublicClientApplication.OperatingSystemAccount);
                            await pcaTester.ExecuteAndDisplay(atsBuilder).ConfigureAwait(false);
                        }
                        break;

                    case 12: // Silent with login hint
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();

                            var pca = pcaBuilder.Build();
                            Console.Write("Please enter email of your current OS account e.g. <username>@microsoft.com: ");
                            string email = Console.ReadLine();

                            // get the token in the cache
                            AcquireTokenSilentParameterBuilder atsBuilder = pca.AcquireTokenSilent(PCATester.Scopes, PublicClientApplication.OperatingSystemAccount);
                            await atsBuilder.ExecuteAsync().ConfigureAwait(false);

                            // now go with the login hint
                            atsBuilder = pca.AcquireTokenSilent(PCATester.Scopes, email);
                            await pcaTester.ExecuteAndDisplay(atsBuilder).ConfigureAwait(false);
                        }
                        break;

                    case 13: // Oranizations as authority
                        {
                            PCATester pcaTester = new PCATester();
                            pcaTester.Authority = PCATester.AuthorityOrganizations;
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            await pcaTester.ExecuteAndDisplay(pcaBuilder).ConfigureAwait(false);
                        }
                        break;

                    case 14: // Consumers as authority
                        {
                            PCATester pcaTester = new PCATester();
                            pcaTester.Authority = PCATester.AuthorityConsumers;
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            await pcaTester.ExecuteAndDisplay(pcaBuilder).ConfigureAwait(false);
                        }
                        break;

                    case 15: // Tenant as authority
                        {
                            PCATester pcaTester = new PCATester();
                            pcaTester.Authority = PCATester.AuthorityTenant;
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            await pcaTester.ExecuteAndDisplay(pcaBuilder).ConfigureAwait(false);
                        }
                        break;

                    case 16: // GetAccounts = all
                        {
                            // TODO - check if this is right - no accounts shown
                            PCATester pcaTester = new PCATester();
                            
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            IPublicClientApplication pca = pcaBuilder.Build();
                            await pcaTester.ExecuteAndDisplay(pcaBuilder).ConfigureAwait(false);

                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                            
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("*** Begin GetAccounts ***");
                            if(accounts.Any())
                            {
                                foreach (var acct in accounts)
                                {
                                    Console.WriteLine($"Upn is {acct.Username}");
                                }
                            }
                            Console.WriteLine("*** End GetAccounts ***");
                        }
                        break;

                    case 17: // GetAccounts ListWorkOrSchool
                        {
                            PCATester pcaTester = new PCATester();
                            pcaTester.ListOSAccounts = true;
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            IPublicClientApplication pca = pcaBuilder.Build();
                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("*** Begin GetAccounts - Work Or School ***");
                            if (accounts.Any())
                            {
                                foreach (var acct in accounts)
                                {
                                    Console.WriteLine($"Upn is {acct.Username}");
                                }
                            }
                            Console.WriteLine("*** End GetAccounts - Work Or School ***");
                        }
                        break;

                    case 18: // Remove account
                        {
                            PCATester pcaTester = new PCATester();
                            pcaTester.ListOSAccounts = true;
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            IPublicClientApplication pca = pcaBuilder.Build();
                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                            IAccount acctToRemove = accounts.FirstOrDefault();
                            if(acctToRemove!= null)
                            {
                                await pca.RemoveAsync(acctToRemove).ConfigureAwait(false);
                            }

                            Console.ForegroundColor = ConsoleColor.Green;
                            try
                            {
                                await pca.AcquireTokenSilent(PCATester.Scopes, acctToRemove).ExecuteAsync().ConfigureAwait(false);

                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("--- Failed to remove account ---");
                            }
                            catch (MsalUiRequiredException)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("+++ Successfully removed account +++");
                            }

                        }
                        break;

                    case 19:
                        {
                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            IPublicClientApplication pca = pcaBuilder.Build();
                            Console.Write("Enter your email:");
                            string email = Console.ReadLine();
                            Console.Write("Enter your password (not visible): ");
                            Console.ForegroundColor= Console.BackgroundColor;
                            string pwd = Console.ReadLine();

                            try
                            {
                                var result = await pca.AcquireTokenByUsernamePassword(PCATester.Scopes, email, pwd)
                                                      .ExecuteAsync()
                                                      .ConfigureAwait(false);
                                if(result.AccessToken!= null)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("+++ Success +++");
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("--- Fail ---");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("--- Fail ---");
                                Console.WriteLine(ex.Message);
                            }
                        }
                        break;

                    case 20: // signout
                        {
                            try
                            {
                                PCATester pcaTester = new PCATester();
                                PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                                var pca = pcaBuilder.Build();
                                AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);
                                atiBulider.WithAccount(PublicClientApplication.OperatingSystemAccount);
                                var result = await atiBulider.ExecuteAsync().ConfigureAwait(false);

                                IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true);
                                var acc = accounts.FirstOrDefault();
                                await pca.RemoveAsync(acc).ConfigureAwait(false);

                                try
                                {
                                    // this should throw an exception
                                    await pca.AcquireTokenSilent(PCATester.Scopes, acc).ExecuteAsync().ConfigureAwait(false);
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("--- Fail ---");
                                    Console.WriteLine("Did not signout");
                                }
                                catch (Exception )
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("+++ Success +++");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("--- Fail ---");
                                Console.WriteLine(ex.Message);
                            }
                        }
                        break;
                    case 21: // Switch Account
                        {
                            Console.Clear();
                            Console.WriteLine("Your OS Account will be passed as login hint.");
                            Console.WriteLine("Please switch to different account and login.");
                            Console.WriteLine("Check the account that is returned.");
                            Console.WriteLine("Press any key to continue...");
                            Console.ReadKey();

                            PCATester pcaTester = new PCATester();
                            PublicClientApplicationBuilder pcaBuilder = pcaTester.CreatePcaBuilder();
                            var pca = pcaBuilder.Build();
                            AcquireTokenInteractiveParameterBuilder atiBulider = pca.AcquireTokenInteractive(PCATester.Scopes);
                            atiBulider.WithAccount(PublicClientApplication.OperatingSystemAccount);
                            var result = await atiBulider.ExecuteAsync().ConfigureAwait(false);

                            Console.WriteLine("\r\n");
                            Console.WriteLine("\r\n");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"The access token was returned for {result.Account.Username}");
                        }
                        break;


                    case 0:
                        return;

                }

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }


    }
}
