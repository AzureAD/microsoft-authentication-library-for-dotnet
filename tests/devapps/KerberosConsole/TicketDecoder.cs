// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kerberos.NET;
using Kerberos.NET.Entities;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace KerberosConsole
{
    /// <summary>
    /// Utility class to handle the Kerberos Ticket.
    /// The Kerberos.NET package is used to show detailed internal structure of a Kerberos Ticket.
    /// You can get detailed information for the Kerberos.NET package here:
    ///    https://www.nuget.org/packages/Kerberos.NET/
    /// </summary>
    public class TicketDecoder
    {
        /// <summary>
        /// Shows the Kerberos Ticket included in an authentication token with KrbCred format
        /// which is used to transfer Kerberos credentials between applications.
        /// Reference: 
        ///     The Unencrypted Form of Kerberos 5 KRB-CRED Message
        ///     https://tools.ietf.org/html/rfc6448
        /// </summary>
        /// <param name="message"></param>
        internal void ShowKrbCredTicket(string message)
        {
            var krbAsRepBytes = Convert.FromBase64String(message);
            var krbCred = KrbCred.DecodeApplication(krbAsRepBytes);
            Assert.IsNotNull(krbCred);

            var credPart = krbCred.Validate();
            Assert.IsNotNull(credPart);

            AADKerberosLogger.PrintLines(2);
            AADKerberosLogger.Save("KRB-CRED Supplemental Ticket -----------------------------");
            AADKerberosLogger.Save("  ProtocolVersionNumber: " + krbCred.ProtocolVersionNumber);
            AADKerberosLogger.Save("  Message Type: " + krbCred.MessageType);
            AADKerberosLogger.Save("  # of Tickets: " + krbCred.Tickets.Length);

            for (int i = 0; i < krbCred.Tickets.Length; i++)
            {
                var ticket = krbCred.Tickets[i];
                var ticketInfo = credPart.TicketInfo[i];

                var key = new byte[ticketInfo.Key.KeyValue.Length];
                ticketInfo.Key.KeyValue.CopyTo(key);

                AADKerberosLogger.Save("  Number: " + ticket.TicketNumber);
                AADKerberosLogger.Save("  Realm: " + ticket.Realm);
                AADKerberosLogger.Save("  SName: " + ticket.SName.FullyQualifiedName);
                ShowEncryptedDataPart("EncryptedPart", ticket.EncryptedPart);

                AADKerberosLogger.Save("  Ticket.Flags: " + ticketInfo.Flags);
                AADKerberosLogger.Save("  Ticket.Realm: " + ticketInfo.Realm);
                AADKerberosLogger.Save("  Ticket.PName: " + ticketInfo.PName.FullyQualifiedName);
                AADKerberosLogger.Save("  Ticket.SRealm: " + ticketInfo.SRealm);
                AADKerberosLogger.Save("  Ticket.SName: " + ticketInfo.SName.FullyQualifiedName);
                AADKerberosLogger.Save("  Ticket.AuthTime: " + ticketInfo.AuthTime);
                AADKerberosLogger.Save("  Ticket.StartTime: " + ticketInfo.StartTime);
                AADKerberosLogger.Save("  Ticket.EndTime: " + ticketInfo.EndTime);
                AADKerberosLogger.Save("  Ticket.RenewTill: " + ticketInfo.RenewTill);
                ShowEncrytionKey("Ticket.Key", ticketInfo.Key);

                if (ticketInfo.AuthorizationData == null)
                {
                     AADKerberosLogger.Save("  Ticket.AuthorizationData:");
                }
                else
                { 
                    for (int j = 0; j < ticketInfo.AuthorizationData.Length; j++)
                    {
                        ShowAuthorizationData("Ticket.AuthorizationData", ticketInfo.AuthorizationData[j]);
                    }
                }
                 AADKerberosLogger.Save("");
            }
        }

        /// <summary>
        /// Shows the internal information of a cached Kerberos Ticket in current user's Windows Ticket Cache
        /// with KRB_AP_REQ format.
        /// Reference:
        ///     The Kerberos Network Authentication Service (V5)
        ///     https://tools.ietf.org/html/rfc4120#section-3.2.1
        /// </summary>
        /// <param name="messaage"></param>
        internal void ShowApReqTicket(string messaage)
        {
            var tokenBytes = System.Convert.FromBase64String(messaage);
            var contextToken = MessageParser.Parse<KerberosContextToken>(tokenBytes);
            Assert.IsNotNull(contextToken);
            Assert.IsNotNull(contextToken.KrbApReq);

            var req = contextToken.KrbApReq;
            Assert.IsNotNull(req.Ticket);

            AADKerberosLogger.PrintLines(2);
            AADKerberosLogger.Save("AP-REQ Cached Ticket----------------------------------------");
             AADKerberosLogger.Save("  Protocol Version Number: " + req.ProtocolVersionNumber);
             AADKerberosLogger.Save("  MessageType: " + req.MessageType);
             AADKerberosLogger.Save("  ApOptions: " + req.ApOptions);

             AADKerberosLogger.Save("  Ticket.TicketNumber: " + req.Ticket.TicketNumber);
             AADKerberosLogger.Save("  Ticket.Realm: " + req.Ticket.Realm);
             AADKerberosLogger.Save("  Ticket.SName: " + req.Ticket.SName.FullyQualifiedName);
            ShowEncryptedDataPart("Ticket.EncryptedPart", req.Ticket.EncryptedPart);
            ShowEncryptedDataPart("Ticket.Authenticator", req.Authenticator);
        }

        private void ShowEncryptedDataPart(string title, KrbEncryptedData data)
        {
            if (data == null)
            {
                AADKerberosLogger.Save($"  {title}:");
            }
            else
            {
                AADKerberosLogger.Save($"  {title}.EType: " + data.EType);
                AADKerberosLogger.Save($"  {title}.KeyVersionNumber: " + data.KeyVersionNumber);
                AADKerberosLogger.Save($"  {title}.Cipher.Length: " + data.Cipher.Length);
                AADKerberosLogger.Save($"  {title}.Cipher.Value:");
                AADKerberosLogger.PrintBinaryData(data.Cipher.ToArray());
            }
        }

        private void ShowAuthorizationData(string title, KrbAuthorizationData auth)
        {
            if (auth != null)
            {
                AADKerberosLogger.Save($"  {title}.Type: {auth.Type}");
                AADKerberosLogger.Save($"  {title}.Data.Length: {auth.Data.Length}");
                AADKerberosLogger.Save($"  {title}.Data.Value:");
                AADKerberosLogger.PrintBinaryData(auth.Data.ToArray());
            }
        }

        private void ShowEncrytionKey(string title, KrbEncryptionKey key)
        {
            if (key == null)
            {
                 AADKerberosLogger.Save($"  {title}:");
            }
            else
            {
                AADKerberosLogger.Save($"  {title}.Usage: {key.Usage}");
                AADKerberosLogger.Save($"  {title}.EType: {key.EType}");
                AADKerberosLogger.Save($"  {title}.KeyValue.Length: {key.KeyValue.Length}");
                AADKerberosLogger.Save($"  {title}.KeyValue.Value:");
                AADKerberosLogger.PrintBinaryData(key.KeyValue.ToArray());
            }
        }
    }
}
