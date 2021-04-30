// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Kerberos;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Security;

namespace KerberosConsole
{
    /// <summary>
    /// It is a sample console program to show the primary usage of the Azure AD Kerberos Feature.
    /// Usages:
    ///     KerberosConsole -h
    ///         show usage format
    ///     KerberosConsole -cached
    ///         find cached Kerberos Ticket with default name defined in the sample application.
    ///     KerberosConsole -cached -spn {spn name}
    ///         find cached Kerberos ticket matched with given {spn name}
    ///     KerberosConsole
    ///         acquire an authentication token, retrieves the Kerberos Ticket, and cache it into the
    ///         current user's Windows Ticket Cache. You can check cached ticket information with "klist" 
    ///         utility command or "KerberosConsole -cached" command.
    ///     KerberosConsole -spn {spn name} -tenantId { tid} -clientId { cid}
    ///         acquire an authentication token with your application configuration.
    /// Note:
    ///     This application uses the Kerberos.NET package to show detailed information of a cached 
    ///     Kerberos Ticket. You can get detailed information for the Kerberos.NET package here:
    ///         https://www.nuget.org/packages/Kerberos.NET/
    /// </summary>
    class Program
    {
        private string TenantId = "428881af-9ab1-40b3-8158-3611358fff68";
        private string ClientId = "5221b482-2651-4cb3-9ad8-c09a78e4de9e";
        private string KerberosServicePrincipalName = "HTTP/prod.aadkreberos.msal.com";
        private KerberosTicketContainer TicketContainer = KerberosTicketContainer.IdToken;
        private string RedirectUri = "http://localhost:8940/";
        private bool FromCache = false;
        private long LogonId = 0;

        private string UserName = "localAdmin@aadktest.onmicrosoft.com";
        private string UserPassword = string.Empty;

        private string[] PublicAppScopes = 
        {
            "user.read",
            "Application.Read.All"
        };

        private string ClientSecret = string.Empty;
        private string[] ConfidentialAppScopes =
        {
            "https://graph.microsoft.com/.default"
        };

        public static void Main(string[] args)
        {
            Program program = new Program();
            if (args.Length > 0 && !program.ParseCommandLineArguments(args))
            {
                return;
            }

            if (program.FromCache)
            {
                program.ShowCachedTicket();
                return;
            }

             program.AcquireKerberosTicket();
        }

        /// <summary>
        /// Parse the command line arguments and set internal parameters with supplied value.
        /// </summary>
        /// <param name="args">List of commandline arguments.</param>
        /// <returns>True if argument parsing completed. False, if there's an error detected.</returns>

        public bool ParseCommandLineArguments(string[] args)
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
                    PublicAppScopes = args[++i].Split(' ');
                }
                else if (args[i].Equals("-cached", StringComparison.OrdinalIgnoreCase))
                {
                    FromCache = true;
                }
                else if (args[i].Equals("-luid", StringComparison.OrdinalIgnoreCase))
                {
                    ++i;
                    try
                    {
                        LogonId = long.Parse(args[i], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("-luid should be a long number or HEX format string!");
                        Console.WriteLine(ex);
                        return false;
                    }
                }
                else if (args[i].Equals("-secret", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    ClientSecret = args[i];
                }
                else if (args[i].Equals("-upn", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    UserName = args[i];
                }
                else if (args[i].Equals("-password", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    UserPassword = args[i];
                }
                else
                {
                    ShowUsages();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Acquire an authentication token.
        /// If a valid token recevied, then show the received token information with included Kerberos Ticket.
        /// If IdToken is used for token container, cached the received Kerberos Ticket into current user's
        /// Windows Ticket Cache.
        /// </summary>
        public void AcquireKerberosTicket()
        {
            Console.WriteLine("Acquiring authentication token ...");
            AuthenticationResult result;
            if (string.IsNullOrEmpty(ClientSecret))
            {
                if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(UserPassword))
                {
                    result = AcquireTokenFromPublicClient(true);
                }
                else
                {
                    result = AcquireTokenFromPublicClientWithUserPassword(true);
                }
            }
            else
            {
                result = AcquireTokenFromConfidentialClient(true);
            }

            if (result == null)
            {
                Console.WriteLine("Failed to get Access Token");
                return;
            }

            if (string.IsNullOrEmpty(ClientSecret))
            {
                ProcessKerberosTicket(result);
                ShowCachedTicket();
            }
        }

        /// <summary>
        /// Checks there's a Kerberos Ticket in current user's Windows Ticket Cache matched with the service principal name.
        /// If there's a valid ticket, then show the Ticket Information on the console.
        /// </summary>
        /// <returns></returns>
        public bool ShowCachedTicket()
        {
            try
            {
                byte[] ticket
                    = KerberosSupplementalTicketManager.GetKerberosTicketFromCache(KerberosServicePrincipalName, LogonId);
                if (ticket != null && ticket.Length > 32)
                {
                    var encode = Convert.ToBase64String(ticket);

                    AADKerberosLogger.PrintLines(2);
                    AADKerberosLogger.Save($"---Find cached Ticket: {ticket.Length} bytes");
                    AADKerberosLogger.PrintBinaryData(ticket);

                    TicketDecoder decoder = new TicketDecoder();
                    decoder.ShowApReqTicket(encode);
                    return true;
                }

                Console.WriteLine($"There's no ticket associated with '{KerberosServicePrincipalName}'");
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine($"ERROR while finding Kerberos Ticket for '{KerberosServicePrincipalName}': {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Shows the program usages.
        /// </summary>
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
            Console.WriteLine("    -luid {loginId} : sets login id of current user with HEX format.");
            Console.WriteLine("    -secret {application secret} : Acquire token using confidential application configuration.");
            Console.WriteLine("    -upn {username}: Username with UPN format, e.g john@contoso.com..");
            Console.WriteLine("    -password {password}: Password for the given username.");
            Console.WriteLine("    -h : show help for command options");
        }

        /// <summary>
        /// Checks there's a valid Kerberos Ticket information within the received authentication token.
        /// If there's a valid one, show the ticket information and cache it into current user's
        /// Windows Ticket Cache so that it can be shared with other Kerberos-aware applications.
        /// </summary>
        /// <param name="result">The <see cref="AuthenticationResult"/> from token request.</param>
        private void ProcessKerberosTicket(AuthenticationResult result)
        {
            KerberosSupplementalTicket ticket;
            if (TicketContainer == KerberosTicketContainer.IdToken)
            {
                // 1. Get the Kerberos Ticket contained in the Id Token.
                ticket = KerberosSupplementalTicketManager.FromIdToken(result.IdToken);
                if (ticket == null)
                {
                    AADKerberosLogger.Save("ERROR: There's no Kerberos Ticket information within the IdToken.");
                    return;
                }

                AADKerberosLogger.PrintLines(2);

                try
                {
                    // 2. Save the Kerberos Ticket into current user's Windows Ticket Cache.
                    KerberosSupplementalTicketManager.SaveToCache(ticket, LogonId);
                    AADKerberosLogger.Save("---Kerberos Ticket cached into user's Ticket Cache\n");
                }
                catch (Win32Exception ex)
                {
                    AADKerberosLogger.Save("---Kerberos Ticket caching failed: " + ex.Message);
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
            else
            {
                AADKerberosLogger.PrintLines(2);
                AADKerberosLogger.Save("Kerberos Ticket handling is not supported for access token.");
            }
        }

        /// <summary>
        /// Acquire an authentication token with public client configuration.
        /// </summary>
        /// <param name="showTokenInfo">Set true to show the acuired token information.</param>
        /// <returns></returns>
        private AuthenticationResult AcquireTokenFromPublicClient(bool showTokenInfo)
        {
            // 1. Setup pulic client application to get Kerberos Ticket.
            var app = PublicClientApplicationBuilder.Create(ClientId)
                .WithTenantId(TenantId)
                .WithRedirectUri(RedirectUri)
                .WithKerberosTicketClaim(KerberosServicePrincipalName, TicketContainer)
                .WithLogging(LogDelegate, LogLevel.Verbose, true, true)
                .Build();

            try
            {
                AADKerberosLogger.Save("Calling AcquireTokenInteractive() with:");
                AADKerberosLogger.Save("         Tenant Id: " + TenantId);
                AADKerberosLogger.Save("         Client Id: " + ClientId);
                AADKerberosLogger.Save("      Redirect Uri: " + RedirectUri);
                AADKerberosLogger.Save("               spn: " + KerberosServicePrincipalName);
                AADKerberosLogger.Save("  Ticket container: " + TicketContainer);

                // 2. Acquire the authentication token.
                // Kerberos Ticket will be contained in Id Token or Access Token
                // according to specified ticket container parameter.
                AuthenticationResult result = app.AcquireTokenInteractive(PublicAppScopes)
                        .ExecuteAsync()
                        .GetAwaiter()
                        .GetResult();

                if (showTokenInfo)
                {
                    ShowAccount(result.Account);
                    AADKerberosLogger.Save("Token Information:");
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

        /// <summary>
        /// Acquire an authentication token with public client configuration using username/password.
        /// 
        /// NOTE: To use this flow, you have to enable "Allow public client flows" in the application 
        /// registration page of the Azure Portal.
        /// </summary>
        /// <param name="showTokenInfo">Set true to show the acuired token information.</param>
        /// <returns></returns>
        private AuthenticationResult AcquireTokenFromPublicClientWithUserPassword(bool showTokenInfo)
        {
            // 1. Setup pulic client application to get Kerberos Ticket.
            var app = PublicClientApplicationBuilder.Create(ClientId)
                .WithTenantId(TenantId)
                .WithRedirectUri(RedirectUri)
                .WithKerberosTicketClaim(KerberosServicePrincipalName, TicketContainer)
                .WithLogging(LogDelegate, LogLevel.Verbose, true, true)
                .Build();

            try
            {
                AADKerberosLogger.Save("Calling AcquireTokenByUsernamePassword() with:");
                AADKerberosLogger.Save("         Tenant Id: " + TenantId);
                AADKerberosLogger.Save("         Client Id: " + ClientId);
                AADKerberosLogger.Save("      Redirect Uri: " + RedirectUri);
                AADKerberosLogger.Save("               spn: " + KerberosServicePrincipalName);
                AADKerberosLogger.Save("  Ticket container: " + TicketContainer);
                AADKerberosLogger.Save("          Username: " + UserName);

                // 2. Acquire the authentication token.
                // Kerberos Ticket will be contained in Id Token or Access Token
                // according to specified ticket container parameter.
                SecureString password = new SecureString();
                foreach (var c in UserPassword.ToCharArray())
                {
                    password.AppendChar(c);
                }

                AuthenticationResult result = app.AcquireTokenByUsernamePassword(PublicAppScopes, UserName, password)
                        .ExecuteAsync()
                        .GetAwaiter()
                        .GetResult();

                if (showTokenInfo)
                {
                    ShowAccount(result.Account);
                    AADKerberosLogger.Save("Token Information:");
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

        /// <summary>
        /// Acquire an authentication token with confidential client configuration.
        /// </summary>
        /// <param name="showTokenInfo">Set true to show the acuired token information.</param>
        /// <returns></returns>
        private AuthenticationResult AcquireTokenFromConfidentialClient(bool showTokenInfo)
        {
            // 1. Setup confidential client application to get Kerberos Ticket.
            // NOTE: We will use AcquireTokenForClient() API to get token for the application which uses the application cache.
            // The id token is for the user and the access token is for the application. With this reason, we have to use AccessToken
            // as the Kerberos Token container for this case.
            var app = ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithRedirectUri(RedirectUri)
                .WithClientSecret(ClientSecret)
                .WithAuthority("https://login.microsoftonline.com/" + TenantId)
                .WithKerberosTicketClaim(KerberosServicePrincipalName, KerberosTicketContainer.AccessToken)
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

                // 2. Acquire the authentication token.
                // Kerberos Ticket will be contained in Id Token or Access Token
                // according to specified ticket container parameter.
                AuthenticationResult result = app.AcquireTokenForClient(ConfidentialAppScopes)
                        .ExecuteAsync()
                        .GetAwaiter()
                        .GetResult();

                if (showTokenInfo)
                {
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

        /// <summary>
        /// Shows the account information included in the authentication result.
        /// </summary>
        /// <param name="account">The <see cref="IAccount"/> information to display.</param>
        private void ShowAccount(IAccount account)
        {
            if (account != null)
            {
                AADKerberosLogger.Save("Account Info:");
                AADKerberosLogger.Save("                 Username: " + account.Username);
                AADKerberosLogger.Save("              Environment: " + account.Environment);
                AADKerberosLogger.Save("    HomeAccount Tenant Id: " + account.HomeAccountId.TenantId);
                AADKerberosLogger.Save("    HomeAccount Object Id: " + account.HomeAccountId.ObjectId);
                AADKerberosLogger.Save("  Home Account Identifier: " + account.HomeAccountId.Identifier);
            }
        }

        /// <summary>
        /// Callback to receive logging message for internal operation of the MSAL.
        /// Show the received message to the console and save to the logging file.
        /// </summary>
        /// <param name="level">Log level of the log message to process</param>
        /// <param name="message">Pre-formatted log message</param>
        /// <param name="containsPii">Indicates if the log message contains Organizational Identifiable Information (OII)
        /// or Personally Identifiable Information (PII) nor not.</param>
        private static void LogDelegate(LogLevel level, string message, bool containsPii)
        {
            AADKerberosLogger.Save(message);
        }
    }
}
