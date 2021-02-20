// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kerberos.NET;
using Kerberos.NET.Entities;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace MsalConsole
{
    /// <summary>
    /// Utility class to check Kerberos Ticket
    /// </summary>
    public class TicketDecoder
    {
        internal void ShowKrbCredTicket(string message)
        {
            var krbAsRepBytes = Convert.FromBase64String(message);
            var krbCred = KrbCred.DecodeApplication(krbAsRepBytes);
            Assert.IsNotNull(krbCred);

            var credPart = krbCred.Validate();
            Assert.IsNotNull(credPart);

            Console.WriteLine("\nKRB-CRED Supplemental Ticket -----------------------------");
            Console.WriteLine("  ProtocolVersionNumber: " + krbCred.ProtocolVersionNumber);
            Console.WriteLine("  Message Type: " + krbCred.MessageType);
            Console.WriteLine("  # of Tickets: " + krbCred.Tickets.Length);

            for (int i = 0; i < krbCred.Tickets.Length; i++)
            {
                var ticket = krbCred.Tickets[i];
                var ticketInfo = credPart.TicketInfo[i];

                var key = new byte[ticketInfo.Key.KeyValue.Length];
                ticketInfo.Key.KeyValue.CopyTo(key);

                Console.WriteLine("  Number: " + ticket.TicketNumber);
                Console.WriteLine("  Realm: " + ticket.Realm);
                Console.WriteLine("  SName: " + ticket.SName.FullyQualifiedName);
                ShowEncryptedDataPart("EncryptedPart", ticket.EncryptedPart);

                Console.WriteLine("\n  Ticket.Flags: " + ticketInfo.Flags);
                Console.WriteLine("  Ticket.Realm: " + ticketInfo.Realm);
                Console.WriteLine("  Ticket.PName: " + ticketInfo.PName.FullyQualifiedName);
                Console.WriteLine("  Ticket.SRealm: " + ticketInfo.SRealm);
                Console.WriteLine("  Ticket.SName: " + ticketInfo.SName.FullyQualifiedName);
                Console.WriteLine("  Ticket.AuthTime: " + ticketInfo.AuthTime);
                Console.WriteLine("  Ticket.StartTime: " + ticketInfo.StartTime);
                Console.WriteLine("  Ticket.EndTime: " + ticketInfo.EndTime);
                Console.WriteLine("  Ticket.RenewTill: " + ticketInfo.RenewTill);
                ShowEncrytionKey("Ticket.Key", ticketInfo.Key);

                if (ticketInfo.AuthorizationData == null)
                {
                    Console.WriteLine("  Ticket.AuthorizationData:");
                }
                else
                { 
                    for (int j = 0; j < ticketInfo.AuthorizationData.Length; j++)
                    {
                        ShowAuthorizationData("Ticket.AuthorizationData", ticketInfo.AuthorizationData[j]);
                    }
                }
                Console.WriteLine("");
            }
        }

        internal void ShowApReqTicket(string messaage)
        {
            var tokenBytes = System.Convert.FromBase64String(messaage);
            var contextToken = MessageParser.Parse<KerberosContextToken>(tokenBytes);
            Assert.IsNotNull(contextToken);
            Assert.IsNotNull(contextToken.KrbApReq);

            var req = contextToken.KrbApReq;
            Assert.IsNotNull(req.Ticket);
            Console.WriteLine("\nAP-REQ Cached Ticket----------------------------------------");
            Console.WriteLine("  Protocol Version Number: " + req.ProtocolVersionNumber);
            Console.WriteLine("  MessageType: " + req.MessageType);
            Console.WriteLine("  ApOptions: " + req.ApOptions);

            Console.WriteLine("\n  Ticket.TicketNumber: " + req.Ticket.TicketNumber);
            Console.WriteLine("  Ticket.Realm: " + req.Ticket.Realm);
            Console.WriteLine("  Ticket.SName: " + req.Ticket.SName.FullyQualifiedName);
            ShowEncryptedDataPart("Ticket.EncryptedPart", req.Ticket.EncryptedPart);
            ShowEncryptedDataPart("Ticket.Authenticator", req.Authenticator);
        }

        private void ShowEncryptedDataPart(string title, KrbEncryptedData data)
        {
            if (data == null)
            {
                Console.WriteLine($"  {title}:");
            }
            else
            {
                Console.WriteLine($"  {title}.EType: " + data.EType);
                Console.WriteLine($"  {title}.KeyVersionNumber: " + data.KeyVersionNumber);
                Console.WriteLine($"  {title}.Cipher.Length: " + data.Cipher.Length);
                Console.WriteLine($"  {title}.Cipher.Value:\n{BitConverter.ToString(data.Cipher.ToArray())}");
            }
        }

        private void ShowAuthorizationData(string title, KrbAuthorizationData auth)
        {
            if (auth != null)
            {
                Console.WriteLine($"  {title}.Type: {auth.Type}");
                Console.WriteLine($"  {title}.Data.Length: {auth.Data.Length}");
                Console.WriteLine($"  {title}.Data.Value:\n{BitConverter.ToString(auth.Data.ToArray())}");
            }
        }

        private void ShowEncrytionKey(string title, KrbEncryptionKey key)
        {
            if (key == null)
            {
                Console.WriteLine($"  {title}:");
            }
            else
            {
                Console.WriteLine($"  {title}.Usage: {key.Usage}");
                Console.WriteLine($"  {title}.EType: {key.EType}");
                Console.WriteLine($"  {title}.KeyValue.Length: {key.KeyValue.Length}");
                Console.WriteLine($"  {title}.KeyValue.Value:\n{BitConverter.ToString(key.KeyValue.ToArray())}");
            }
        }
    }
}
