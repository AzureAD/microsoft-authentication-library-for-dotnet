// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kerberos.NET.Win32;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Kerberos;

using System;

namespace KerberosConsole
{
    class Program
    {
        private string TenantId = "428881af-9ab1-40b3-8158-3611358fff68";
        private string ClientId = "5221b482-2651-4cb3-9ad8-c09a78e4de9e";
        private string KerberosServicePrincipalName = "HTTP/prod.aadkreberos.msal.com";
        private KerberosTicketContainer TicketContainer = KerberosTicketContainer.IdToken;
        private string RedirectUri = "http://localhost:8940/";
        private bool FromCache = false;

        private string[] GraphScopes = {
            "user.read",
            "Application.Read.All"
        };

        public static void Main(string[] args)
        {
            Program program = new Program();
            if (args.Length > 0 && !program.Parse(args))
            {
                return;
            }

            if (program.FromCache)
            {
                program.ShowCachedTicket();
                return;
            }

             program.AcquireToken();
        }

        public bool Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-tenantId", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    TenantId = args[++i];
                }
                else if (args[i].Equals("-clientId", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    ClientId = args[++i];
                }
                else if (args[i].Equals("-spn", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    KerberosServicePrincipalName = args[++i];
                }
                else if (args[i].Equals("-container", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    ++i;
                    if (args[i].Equals("id", StringComparison.OrdinalIgnoreCase))
                        TicketContainer = KerberosTicketContainer.IdToken;
                    else if (args[i].Equals("access", StringComparison.OrdinalIgnoreCase))
                        TicketContainer = KerberosTicketContainer.AccessToken;
                    else
                    {
                        Console.WriteLine("Unknown ticket container type '" + args[i] + "'");
                        ShowUsages();
                        return false;
                    }
                }
                else if (args[i].Equals("-redirectUri", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    RedirectUri = args[++i];
                }
                else if (args[i].Equals("-scopes", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    GraphScopes = args[++i].Split(' ');
                }
                else if (args[i].Equals("-cached", StringComparison.OrdinalIgnoreCase))
                {
                    FromCache = true;
                }
                else
                {
                    ShowUsages();
                    return false;
                }
            }

            return true;
        }

        public void AcquireToken()
        {
            Console.WriteLine("Acquiring authentication token ...");
            AuthenticationResult result = ReadAccount(false);
            if (result == null)
            {
                Console.WriteLine("Failed to get Access Token");
                return;
            }

            ShowKerberosTicketDetails(result);
            ShowCachedTicket();
        }

        public bool ShowCachedTicket()
        {
            byte[] ticket = FindCachedTicket(KerberosServicePrincipalName);
            if (ticket != null && ticket.Length > 32)
            {
                var encode = Convert.ToBase64String(ticket);

                AADKerberosLogger.PrintLines(2);
                AADKerberosLogger.Save("---Find cached Ticket:");
                AADKerberosLogger.Save(encode);

                TicketDecoder decoder = new TicketDecoder();
                decoder.ShowApReqTicket(encode);
                return true;
            }

            Console.WriteLine("ERROR: can't find token entry for " + KerberosServicePrincipalName);
            return false;
        }

        private static void ShowUsages()
        {
            Console.WriteLine("KerberosConsole {option}");
            Console.WriteLine("    -tenantId {id} : set tenent Id to use.");
            Console.WriteLine("    -clientId {id} : set client Id (Application Id) to use.");
            Console.WriteLine("    -spn {spn} : set the service principal name to use.");
            Console.WriteLine("    -container {id, access} : set the ticket container to use.");
            Console.WriteLine("    -redirectUri {uri} : set redirectUri for OAuth2 authentication.");
            Console.WriteLine("    -scopes {scopes} : list of scope separated by space.");
            Console.WriteLine("    -cached : check cached ticket first.");
            Console.WriteLine("    -h : show help for command options");
        }

        private void ShowKerberosTicketDetails(AuthenticationResult result)
        {
            KerberosSupplementalTicket ticket;
            if (TicketContainer == KerberosTicketContainer.IdToken)
            {
                ticket = KerberosTicketManager.FromToken(result.IdToken);
            }
            else
            {
                ticket = KerberosTicketManager.FromToken(result.AccessToken);
            }

            if (ticket == null)
            {
                AADKerberosLogger.Save("ERROR: There's no Kerberos Ticket information within the AuthResult.");
                AADKerberosLogger.Save("Access Token: " + result.AccessToken);
            }
            else
            {
                // save ticket to cache.
                if (KerberosTicketManager.SaveToCache(ticket))
                {
                    AADKerberosLogger.PrintLines(2);
                    AADKerberosLogger.Save("---Kerberos Ticket cached into user's Ticket Cache\n");
                }

                AADKerberosLogger.PrintLines(2);
                AADKerberosLogger.Save("KerberosSupplementalTicket {");
                AADKerberosLogger.Save("                Client Key: " + ticket.ClientKey);
                AADKerberosLogger.Save("                  Key Type: " + ticket.KeyType);
                AADKerberosLogger.Save("            Errorr Message: " + ticket.ErrorMessage);
                AADKerberosLogger.Save("                     Realm: " + ticket.Realm);
                AADKerberosLogger.Save("    Service Principal Name: " + ticket.ServicePrincipalName);
                AADKerberosLogger.Save("               Client Name: " + ticket.ClientName);
                AADKerberosLogger.Save("     KerberosMessageBuffer: " + ticket.KerberosMessageBuffer);
                AADKerberosLogger.Save("}\n");

                // shows detailed ticket information.
                TicketDecoder decoder = new TicketDecoder();
                decoder.ShowKrbCredTicket(ticket.KerberosMessageBuffer);
            }
        }

        private byte[] FindCachedTicket(string servicePrincipalName)
        {
            try
            { 
                using (SspiContext context = new SspiContext(servicePrincipalName))
                {
                    return context.RequestToken();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excetion from FindCachedTicket: " + ex.Message + "\n");
                return null;
            }
        }

        private AuthenticationResult ReadAccount(bool showTokenInfo)
        {
            var app = PublicClientApplicationBuilder.Create(ClientId)
                .WithTenantId(TenantId)
                .WithRedirectUri(RedirectUri)
                .WithKerberosServicePrincipal(KerberosServicePrincipalName)
                .WithKerberosTicketContainer(TicketContainer)
                .WithLogging(LogDelegate, LogLevel.Verbose, true, true)
                .Build();

            try
            {
                AADKerberosLogger.Save("Calling AcquireTokenInteractive() with:");
                AADKerberosLogger.Save("         Tenant Id: " + TenantId);
                AADKerberosLogger.Save("         Client Id: " + ClientId);
                AADKerberosLogger.Save("      Redirect Uri: " + RedirectUri);
                AADKerberosLogger.Save("               spn: " + KerberosServicePrincipalName);
                AADKerberosLogger.Save("  ticket container: " + TicketContainer);

                AuthenticationResult result = app.AcquireTokenInteractive(GraphScopes)
                        .ExecuteAsync()
                        .GetAwaiter()
                        .GetResult();

                if (showTokenInfo)
                {
                    ShowAccount(result.Account);
                    AADKerberosLogger.Save("           Correlation Id: " + result.CorrelationId);
                    AADKerberosLogger.Save("                Unique Id:" + result.UniqueId);
                    AADKerberosLogger.Save("                Expres On: " + result.ExpiresOn);
                    AADKerberosLogger.Save("  IsExtendedLifeTimeToken: " + result.IsExtendedLifeTimeToken);
                    AADKerberosLogger.Save("       Extended Expres On: " + result.ExtendedExpiresOn);
                    AADKerberosLogger.Save("             Access Token:\n" + result.AccessToken);
                    AADKerberosLogger.Save("                 Id Token:\n" + result.IdToken);
                }
                return result;
            }
            catch (Exception ex)
            {
                AADKerberosLogger.Save("Exception: " + ex);
                return null;
            }
        }

        private void ShowAccount(IAccount account)
        {
            AADKerberosLogger.Save("                 Username: " + account.Username);
            AADKerberosLogger.Save("              Environment: " + account.Environment);
            AADKerberosLogger.Save("    HomeAccount Tenant Id: " + account.HomeAccountId.TenantId);
            AADKerberosLogger.Save("    HomeAccount Object Id: " + account.HomeAccountId.ObjectId);
            AADKerberosLogger.Save("  Home Account Identifier: " + account.HomeAccountId.Identifier);
        }

        private static void LogDelegate(LogLevel level, string message, bool containsPii)
        {
            AADKerberosLogger.Save(message);
        }
    }
}
