// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Kerberos;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NSubstitute;

namespace Microsoft.Identity.Test.Common
{
    internal static class TestCommon
    {
        public static void ResetInternalStaticCaches()
        {
            // This initializes the classes so that the statics inside them are fully initialized, and clears any cached content in them.
            new InstanceDiscoveryManager(
                Substitute.For<IHttpManager>(),
                true, null, null);
            new AuthorityResolutionManager(true);
            SingletonThrottlingManager.GetInstance().ResetCache();
        }

        public static object GetPropValue(object src, string propName)
        {
            object result = null;
            try
            {
                result = src.GetType().GetProperty(propName).GetValue(src, null);
            }
            catch
            {
                Console.WriteLine($"Property with name {propName}");
            }

            return result;
        }

        public static IServiceBundle CreateServiceBundleWithCustomHttpManager(
            IHttpManager httpManager,
            TelemetryCallback telemetryCallback = null,
            LogCallback logCallback = null,
            string authority = ClientApplicationBase.DefaultAuthority,
            bool isExtendedTokenLifetimeEnabled = false,
            bool enablePiiLogging = false,
            string clientId = TestConstants.ClientId,
            bool clearCaches = true,
            bool validateAuthority = true,
            bool isLegacyCacheEnabled = true)
        {
            
            var appConfig = new ApplicationConfiguration()
            {
                ClientId = clientId,
                HttpManager = httpManager,
                RedirectUri = PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(clientId),
                TelemetryCallback = telemetryCallback,
                LoggingCallback = logCallback,
                LogLevel = LogLevel.Verbose,
                EnablePiiLogging = enablePiiLogging,
                IsExtendedTokenLifetimeEnabled = isExtendedTokenLifetimeEnabled,
                AuthorityInfo = AuthorityInfo.FromAuthorityUri(authority, validateAuthority),
                LegacyCacheCompatibilityEnabled = isLegacyCacheEnabled
            };            
            return new ServiceBundle(appConfig, clearCaches);
        }

        public static IServiceBundle CreateDefaultServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null);
        }

        public static IServiceBundle CreateDefaultAdfsServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null, authority: TestConstants.OnPremiseAuthority);
        }

        public static AuthenticationRequestParameters CreateAuthenticationRequestParameters(
            IServiceBundle serviceBundle,
            Authority authority = null,
            HashSet<string> scopes = null,
            RequestContext requestContext = null)
        {
            var commonParameters = new AcquireTokenCommonParameters
            {
                Scopes = scopes ?? TestConstants.s_scope,
            };

            authority = authority ?? Authority.CreateAuthority(TestConstants.AuthorityTestTenant);
            requestContext = requestContext ?? new RequestContext(serviceBundle, Guid.NewGuid())
            {
                ApiEvent = new Client.TelemetryCore.Internal.Events.ApiEvent(
                    serviceBundle.ApplicationLogger,
                    serviceBundle.PlatformProxy.CryptographyManager,
                    Guid.NewGuid().ToString())
            };

            return new AuthenticationRequestParameters(
                serviceBundle,
                new TokenCache(serviceBundle, false),
                commonParameters,
                requestContext,
                authority)
            {                
            };
        }

        /// <summary>
        /// Get a Kerberos Ticket contained in the given <see cref="AuthenticationResult"/> object.
        /// </summary>
        /// <param name="authResult">An <see cref="AuthenticationResult"/> object to get Kerberos Ticket from.</param>
        /// <param name="container">The <see cref="KerberosTicketContainer"/> indicating the token where the Kerberos Ticket stored.</param>"
        /// <param name="userUpn">UPN of the client.</param>
        /// <returns>A <see cref="KerberosSupplementalTicket"/> if there's valid one.</returns>
        public static KerberosSupplementalTicket GetValidatedKerberosTicketFromAuthenticationResult(AuthenticationResult authResult,
            KerberosTicketContainer container,
            string userUpn)
        {
            if (container == KerberosTicketContainer.IdToken)
            {
                ValidateNoKerberosTicketFromToken(authResult.AccessToken);
                return GetValidatedKerberosTicketFromToken(authResult.IdToken, userUpn);
            }

            ValidateNoKerberosTicketFromToken(authResult.IdToken);
            return GetValidatedKerberosTicketFromToken(authResult.AccessToken, userUpn);
        }

        /// <summary>
        /// Get a Kerberos Ticket contained in the given token.
        /// </summary>
        /// <param name="token">Token to be validated.</param>
        /// <param name="userUpn">UPN of the client.</param>
        /// <returns>A <see cref="KerberosSupplementalTicket"/> if there's valid one.</returns>
        public static KerberosSupplementalTicket GetValidatedKerberosTicketFromToken(string token, string userUpn)
        {
            KerberosSupplementalTicket ticket = KerberosSupplementalTicketManager.FromIdToken(token);

            Assert.IsNotNull(ticket, "Kerberos Ticket is not found.");
            Assert.IsTrue(string.IsNullOrEmpty(ticket.ErrorMessage), "Kerberos Ticket creation failed with: " + ticket.ErrorMessage);
            Assert.IsFalse(string.IsNullOrEmpty(ticket.KerberosMessageBuffer), "Kerberos Ticket data is not found.");
            Assert.IsTrue(ticket.KerberosMessageBuffer.Length > TestConstants.KerberosMinMessageBufferLength, "Received Kerberos Ticket data is too short.");
            Assert.AreEqual(KerberosKeyTypes.Aes256CtsHmacSha196, ticket.KeyType, "Kerberos key type is not matched.");
            Assert.AreEqual(TestConstants.KerberosServicePrincipalName, ticket.ServicePrincipalName, true, CultureInfo.InvariantCulture, "Service principal name is not matched.");
            Assert.AreEqual(userUpn, ticket.ClientName, true, CultureInfo.InvariantCulture, "Client name is not matched.");

            return ticket;
        }

        /// <summary>
        /// Validates Windows Ticket Cache interface for the given <see cref="KerberosSupplementalTicket"/> Kerberos Ticket.
        /// </summary>
        /// <param name="ticket">A <see cref="KerberosSupplementalTicket"/> object to be checked.</param>
        public static void ValidateKerberosWindowsTicketCacheOperation(KerberosSupplementalTicket ticket)
        {
            if (DesktopOsHelper.IsWindows())
            {
                // First, save the given Kerberos Ticket (with KRB-CRED format) into the Windows Ticket Cache.
                // Windows Ticket Cache decrypts the given Kerberos Ticket with KRB-CRED format, re-encrypt with it's
                // credential and save it as AP-REQ format.
                KerberosSupplementalTicketManager.SaveToWindowsTicketCache(ticket);

                // Read-back saved Ticket data.
                byte[] ticketBytes
                    = KerberosSupplementalTicketManager.GetKerberosTicketFromWindowsTicketCache(ticket.ServicePrincipalName);
                Assert.IsNotNull(ticketBytes);

                // To validate public field of AP-REQ format Kerberos Ticket, convert binary ticket data as a printable string format.
                StringBuilder sb = new StringBuilder();
                foreach (byte ch in ticketBytes)
                {
                    if (ch >= 32 && ch < 127)
                    {
                        sb.Append((char)ch);
                    }
                    else
                    {
                        sb.Append('*');
                    }
                }
                string ticketAsString = sb.ToString();

                // Check the Azure AD Kerberos Realm string exists.
                Assert.IsTrue(ticketAsString.IndexOf(TestConstants.AzureADKerberosRealmName) >= 0);

                // Check the ticket has matched Kerberos Service Principal Name.
                Assert.IsTrue(ticketAsString.IndexOf(TestConstants.KerberosServicePrincipalNameEscaped, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        /// <summary>
        /// Validates there were no Kerberos Ticket with the given <see cref="AuthenticationResult"/> object.
        /// </summary>
        /// <param name="authResult">An <see cref="AuthenticationResult"/> object to be checked.</param>
        public static void ValidateNoKerberosTicketFromAuthenticationResult(AuthenticationResult authResult)
        {
            ValidateNoKerberosTicketFromToken(authResult.IdToken);
            ValidateNoKerberosTicketFromToken(authResult.AccessToken);
        }

        /// <summary>
        /// Validates there were no valid Kerberos Ticket contained in the given token.
        /// </summary>
        /// <param name="token">Token to be validated.</param>
        public static void ValidateNoKerberosTicketFromToken(string token)
        {
            KerberosSupplementalTicket ticket = KerberosSupplementalTicketManager.FromIdToken(token);
            Assert.IsNull(ticket, "Kerberos Ticket exists.");
        }
    }
}
