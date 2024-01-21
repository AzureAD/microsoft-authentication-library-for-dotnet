// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Kerberos;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Security;
using System.Threading.Tasks;

namespace KerberosConsole
{
    /// <summary>
    /// It is a sample console program to show the primary usage of the Azure AD Kerberos Feature.
    /// Usages:
    ///     KerberosConsole -h
    ///         show usage format.
    ///     KerberosConsole -cached
    ///         find cached Kerberos Ticket with default name defined in the sample application.
    ///     KerberosConsole -cached -spn {spn name}
    ///         find cached Kerberos ticket matched with given {spn name}.
    ///     KerberosConsole
    ///         acquire an authentication token, retrieves the Kerberos Ticket, and cache it into the
    ///         current user's Windows Ticket Cache. You can check cached ticket information with "klist" 
    ///         utility command or "KerberosConsole -cached" command.
    ///     KerberosConsole -devicecodeflow
    ///         acquire an authentication token using the device code flow.
    ///     KerberosConsole -upn john@contoso.com -password passwordxxx
    ///         acquire an authentication token using Username/Password flow.
    ///         You have to provide both the upn and password.
    /// Note:
    ///     This application uses the Kerberos.NET package to show detailed information of a cached 
    ///     Kerberos Ticket. You can get detailed information for the Kerberos.NET package here:
    ///         https://www.nuget.org/packages/Kerberos.NET/
    /// </summary>
    class Program
    {
        private string _tenantId = "428881af-9ab1-40b3-8158-3611358fff68";
        private string _clientId = "5221b482-2651-4cb3-9ad8-c09a78e4de9e";
        private string _kerberosServicePrincipalName = "HTTP/prod.aadkreberos.msal.com";
        private KerberosTicketContainer _ticketContainer = KerberosTicketContainer.IdToken;
        private string _redirectUri = "http://localhost:8940/";
        private bool _isReadFromCache = false;
        private long _logonId = 0;

        private string _Username = "localAdmin@aadktest.onmicrosoft.com";
        private string _UserPassword = string.Empty;

        private bool _isDeviceCodeFlow = false;

        private string[] _publicAppScopes = 
        {
            "user.read",
            "Application.Read.All"
        };

        public static void Main(string[] args)
        {
            Program program = new Program();
            if (args.Length > 0 && !program.ParseCommandLineArguments(args))
            {
                return;
            }

            if (program._isReadFromCache)
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
                    _tenantId = args[++i];
                }
                else if (args[i].Equals("-clientId", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    _clientId = args[++i];
                }
                else if (args[i].Equals("-spn", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    _kerberosServicePrincipalName = args[++i];
                }
                else if (args[i].Equals("-container", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    ++i;
                    if (args[i].Equals("id", StringComparison.OrdinalIgnoreCase))
                        _ticketContainer = KerberosTicketContainer.IdToken;
                    else if (args[i].Equals("access", StringComparison.OrdinalIgnoreCase))
                        _ticketContainer = KerberosTicketContainer.AccessToken;
                    else
                    {
                        Console.WriteLine("Unknown ticket container type '" + args[i] + "'");
                        ShowUsages();
                        return false;
                    }
                }
                else if (args[i].Equals("-redirectUri", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    _redirectUri = args[++i];
                }
                else if (args[i].Equals("-scopes", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    _publicAppScopes = args[++i].Split(' ');
                }
                else if (args[i].Equals("-cached", StringComparison.OrdinalIgnoreCase))
                {
                    _isReadFromCache = true;
                }
                else if (args[i].Equals("-luid", StringComparison.OrdinalIgnoreCase))
                {
                    ++i;
                    try
                    {
                        _logonId = long.Parse(args[i], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("-luid should be a long number or HEX format string!");
                        Console.WriteLine(ex);
                        return false;
                    }
                }
                else if (args[i].Equals("-upn", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    _Username = args[i];
                }
                else if (args[i].Equals("-password", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    _UserPassword = args[i];
                }
                else if (args[i].Equals("-devicecodeflow", StringComparison.OrdinalIgnoreCase))
                {
                    _isDeviceCodeFlow = true;
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
        /// If a valid token received, then show the received token information with included Kerberos Ticket.
        /// If IdToken is used for token container, cached the received Kerberos Ticket into current user's
        /// Windows Ticket Cache.
        /// </summary>
        public void AcquireKerberosTicket()
        {
            Console.WriteLine("Acquiring authentication token ...");
            AuthenticationResult result;
            if (_isDeviceCodeFlow)
            {
                result = AcquireTokenWithDeviceCodeFlow();
            }
            else if (string.IsNullOrEmpty(_Username) || string.IsNullOrEmpty(_UserPassword))
            {
                result = AcquireTokenFromPublicClientWithInteractive();
            }
            else
            {
                result = AcquireTokenFromPublicClientWithUserPassword();
            }

            if (result == null)
            {
                Console.WriteLine("Failed to get Access Token");
                return;
            }

            ProcessKerberosTicket(result);
            ShowCachedTicket();
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
                    = KerberosSupplementalTicketManager.GetKerberosTicketFromWindowsTicketCache(_kerberosServicePrincipalName, _logonId);
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

                Console.WriteLine($"There's no ticket associated with '{_kerberosServicePrincipalName}'");
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine($"ERROR while finding Kerberos Ticket for '{_kerberosServicePrincipalName}': {ex.Message}");
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
            Console.WriteLine("    -upn {username}: Username with UPN format, e.g john@contoso.com, for Username/Password based authentication flow.");
            Console.WriteLine("    -password {password}: Password for the given username.");
            Console.WriteLine("    -devicecodeflow: Use Device Code flow to get authentication token.");
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
            if (_ticketContainer == KerberosTicketContainer.IdToken)
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
                    KerberosSupplementalTicketManager.SaveToWindowsTicketCache(ticket, _logonId);
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
        /// Acquire an authentication token interactively with public client configuration.
        /// </summary>
        /// <returns>The <see cref="AuthenticationResult"/> object.</returns>
        private AuthenticationResult AcquireTokenFromPublicClientWithInteractive()
        {
            // 1. Setup public client application to get Kerberos Ticket.
            var app = PublicClientApplicationBuilder.Create(_clientId)
                .WithTenantId(_tenantId)
                .WithRedirectUri(_redirectUri)
                .WithKerberosTicketClaim(_kerberosServicePrincipalName, _ticketContainer)
                .WithLogging(LogDelegate, LogLevel.Verbose, true, true)
                .Build();

            try
            {
                AADKerberosLogger.Save("Calling AcquireTokenInteractive() with:");
                AADKerberosLogger.Save("         Tenant Id: " + _tenantId);
                AADKerberosLogger.Save("         Client Id: " + _clientId);
                AADKerberosLogger.Save("      Redirect Uri: " + _redirectUri);
                AADKerberosLogger.Save("               spn: " + _kerberosServicePrincipalName);
                AADKerberosLogger.Save("  Ticket container: " + _ticketContainer);

                // 2. Acquire the authentication token.
                // Kerberos Ticket will be contained in Id Token or Access Token
                // according to specified ticket container parameter.
                AuthenticationResult result = app.AcquireTokenInteractive(_publicAppScopes)
                        .ExecuteAsync()
                        .GetAwaiter()
                        .GetResult();

                ShowAuthenticationResult(result);
                return result;
            }
            catch (Exception ex)
            {
                AADKerberosLogger.Save("Exception: " + ex);
                return null;
            }
        }

        /// <summary>
        /// Acquire an authentication token with public client configuration using username/password non-interactively.
        /// 
        /// NOTE: You have to enable public client flows for your application in the Azure Portal:
        ///     Login to the Azure Portal
        ///     Goto "App Registrations"
        ///     Select your application
        ///     Select "Authentication" under the Manage section.
        ///     Select "Yes" for the "Allow public client flows" under the Advanced Settings.
        ///     Click "Save" to save the changes.
        /// </summary>
        /// <returns>The <see cref="AuthenticationResult"/> object.</returns>
        private AuthenticationResult AcquireTokenFromPublicClientWithUserPassword()
        {
            // 1. Setup public client application to get Kerberos Ticket.
            var app = PublicClientApplicationBuilder.Create(_clientId)
                .WithTenantId(_tenantId)
                .WithRedirectUri(_redirectUri)
                .WithKerberosTicketClaim(_kerberosServicePrincipalName, _ticketContainer)
                .WithLogging(LogDelegate, LogLevel.Verbose, true, true)
                .Build();

            try
            {
                AADKerberosLogger.Save("Calling AcquireTokenByUsernamePassword() with:");
                AADKerberosLogger.Save("         Tenant Id: " + _tenantId);
                AADKerberosLogger.Save("         Client Id: " + _clientId);
                AADKerberosLogger.Save("      Redirect Uri: " + _redirectUri);
                AADKerberosLogger.Save("               spn: " + _kerberosServicePrincipalName);
                AADKerberosLogger.Save("  Ticket container: " + _ticketContainer);
                AADKerberosLogger.Save("          Username: " + _Username);

                // 2. Acquire the authentication token.
                // Kerberos Ticket will be contained in Id Token or Access Token
                // according to specified ticket container parameter.
                AuthenticationResult result = app.AcquireTokenByUsernamePassword(_publicAppScopes, _Username, _UserPassword)
                        .ExecuteAsync()
                        .GetAwaiter()
                        .GetResult();

                ShowAuthenticationResult(result);
                return result;
            }
            catch (Exception ex)
            {
                AADKerberosLogger.Save("Exception: " + ex);
                return null;
            }
        }

        /// <summary>
        /// Acquire an authentication token with public client configuration using username/password non-interactively.
        /// </summary>
        /// <returns>The <see cref="AuthenticationResult"/> object.</returns>
        private AuthenticationResult AcquireTokenWithDeviceCodeFlow()
        {
            // 1. Setup public client application to get Kerberos Ticket.
            var app = PublicClientApplicationBuilder.Create(_clientId)
                .WithTenantId(_tenantId)
                .WithRedirectUri(_redirectUri)
                .WithKerberosTicketClaim(_kerberosServicePrincipalName, _ticketContainer)
                .WithLogging(LogDelegate, LogLevel.Verbose, true, true)
                .Build();

            try
            {
                AADKerberosLogger.Save("Calling AcquireTokenWithDeviceCodeFlow() with:");
                AADKerberosLogger.Save("         Tenant Id: " + _tenantId);
                AADKerberosLogger.Save("         Client Id: " + _clientId);
                AADKerberosLogger.Save("      Redirect Uri: " + _redirectUri);
                AADKerberosLogger.Save("               spn: " + _kerberosServicePrincipalName);
                AADKerberosLogger.Save("  Ticket container: " + _ticketContainer);

                // 2. Acquire the authentication token.
                // Kerberos Ticket will be contained in Id Token or Access Token
                // according to specified ticket container parameter.
                AuthenticationResult result = app
                    .AcquireTokenWithDeviceCode(
                        _publicAppScopes,
                        deviceCodeCallback =>
                        {
                            ConsoleColor current = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(deviceCodeCallback.Message);
                            Console.ForegroundColor = current;

                            // stop console output of logging information posted from the MSAL.
                            AADKerberosLogger.SkipLoggingToConsole = true;

                            return Task.FromResult(0);
                        })
                    .ExecuteAsync()
                    .GetAwaiter()
                    .GetResult();

                AADKerberosLogger.SkipLoggingToConsole = false;
                ShowAuthenticationResult(result);
                return result;
            }
            catch (Exception ex)
            {
                AADKerberosLogger.Save("Exception: " + ex);
                return null;
            }
        }

        private void ShowAuthenticationResult(AuthenticationResult result)
        {
            ConsoleColor current = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            ShowAccount(result.Account);
            AADKerberosLogger.Save("Token Information:");
            AADKerberosLogger.Save("           Correlation Id: " + result.CorrelationId);
            AADKerberosLogger.Save("                Unique Id:" + result.UniqueId);
            AADKerberosLogger.Save("                Expres On: " + result.ExpiresOn);
            AADKerberosLogger.Save("             Access Token:\n" + result.AccessToken);
            AADKerberosLogger.Save("                 Id Token:\n" + result.IdToken);

            Console.ForegroundColor = current;
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
