// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kerberos.NET.Win32;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Kerberos;

using System;

namespace MsalConsole
{
    class Program
    {
        private string TenantId = "428881af-9ab1-40b3-8158-3611358fff68";
        private string ClientId = "5221b482-2651-4cb3-9ad8-c09a78e4de9e";
        private string KerberosServicePrincipalName = "HTTP/prod.aadkreberos.msal.com";
        private string RedirectUrl = "http://localhost:8940/";
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
                    this.TenantId = args[++i];
                }
                else if (args[i].Equals("-clientId", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    this.ClientId = args[++i];
                }
                else if (args[i].Equals("-spn", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    this.KerberosServicePrincipalName = args[++i];
                }
                else if (args[i].Equals("-redirectUri", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    this.RedirectUrl = args[++i];
                }
                else if (args[i].Equals("-scopes", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    this.GraphScopes = args[++i].Split(' ');
                }
                else if (args[i].Equals("-cached", StringComparison.OrdinalIgnoreCase))
                {
                    this.FromCache = true;
                }
                else
                {
                    Console.WriteLine("MsalConsole {option}");
                    Console.WriteLine("    -tenantId {id} : set tenent Id to use.");
                    Console.WriteLine("    -clientId {id} : set client Id (Application Id) to use.");
                    Console.WriteLine("    -redirectUri {uri} : set redirectUri for OAuth2 authentication.");
                    Console.WriteLine("    -scopes {scopes} : list of scope separated by space.");
                    Console.WriteLine("    -cached : check cached ticket first.");
                    Console.WriteLine("    -h : show help for command options");
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

            ShowKerberosSupplementalTicket(result.KerberosSupplementalTicket);
            ShowCachedTicket();
        }

        public bool ShowCachedTicket()
        {
            byte[] ticket = FindCachedTicket(this.KerberosServicePrincipalName);
            if (ticket != null && ticket.Length > 32)
            {
                var encode = Convert.ToBase64String(ticket);
                AADKerberosLogger.Save("---Find cached Ticket:");
                AADKerberosLogger.Save(encode);

                TicketDecoder decoder = new TicketDecoder();
                decoder.ShowApReqTicket(encode);
                return true;
            }

            Console.WriteLine("ERROR: can't find token entry for " + KerberosServicePrincipalName);
            return false;
        }

        private void ShowKerberosSupplementalTicket(MsalKerberosSupplementalTicket ticket)
        {
            AADKerberosLogger.Save("\nKerberosSupplementalTicket {");
            AADKerberosLogger.Save("                Client Key: " + ticket.ClientKey);
            AADKerberosLogger.Save("                  Key Type: " + ticket.KeyType);
            AADKerberosLogger.Save("            Errorr Message: " + ticket.ErrorMessage);
            AADKerberosLogger.Save("                     Realm: " + ticket.Realm);
            AADKerberosLogger.Save("    Service Principal Name: " + ticket.ServicePrincipalName);
            AADKerberosLogger.Save("               Client Name: " + ticket.ClientName);
            AADKerberosLogger.Save("     KerberosMessageBuffer: " + ticket.KerberosMessageBuffer);
            AADKerberosLogger.Save("}\n");

            TicketDecoder decoder = new TicketDecoder();
            decoder.ShowKrbCredTicket(ticket.KerberosMessageBuffer);
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
            var app = PublicClientApplicationBuilder.Create(this.ClientId)
                .WithTenantId(this.TenantId)
                .WithRedirectUri(this.RedirectUrl)
                .WithKerberosServicePrincipal(this.KerberosServicePrincipalName)
                .WithLogging(LogDelegate, LogLevel.Verbose, true, true)
                .Build();

            try
            {
                AuthenticationResult result = app.AcquireTokenInteractive(this.GraphScopes)
                        .ExecuteAsync()
                        .GetAwaiter()
                        .GetResult();

                if (showTokenInfo)
                {
                    ShowAccount(result.Account);
                    Console.WriteLine("Correlation Id: " + result.CorrelationId);
                    Console.WriteLine("Unique Id :" + result.UniqueId);
                    Console.WriteLine("Expres On: " + result.ExpiresOn);
                    Console.WriteLine("IsExtendedLifeTimeToken: " + result.IsExtendedLifeTimeToken);
                    Console.WriteLine("Extended Expres On: " + result.ExtendedExpiresOn);
                    Console.WriteLine("Access Token:\n" + result.AccessToken);
                    Console.WriteLine("Id Token:\n" + result.IdToken);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
                return null;
            }
        }

        private void ShowAccount(IAccount account)
        {
            Console.WriteLine("Username: " + account.Username);
            Console.WriteLine("Environment: " + account.Environment);
            Console.WriteLine("HomeAccount Tenant Id: " + account.HomeAccountId.TenantId);
            Console.WriteLine("HomeAccount Object Id: " + account.HomeAccountId.ObjectId);
            Console.WriteLine("Home Account Identifier: " + account.HomeAccountId.Identifier);
        }

        private static void LogDelegate(LogLevel level, string message, bool containsPii)
        {
            AADKerberosLogger.Save(message);
        }
    }
}
