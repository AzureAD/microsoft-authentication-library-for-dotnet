// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Diagnostics;

namespace Microsoft.Identity.Extensions.Mac
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    internal class MacOSKeychainCredential 
    {
        internal MacOSKeychainCredential(string service, string account, byte[] password, string label)
        {
            Service = service;
            Account = account;
            Password = password;
            Label = label;
        }

        public string Service { get; }

        public string Account { get; }

        public string Label { get; }

        public byte[] Password { get; }

        private string DebuggerDisplay => $"{Label} [Service: {Service}, Account: {Account}]";
    }
}
