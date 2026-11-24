// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

// Model only the keychain API used by the linked accessor. Mobile builds still validate the native APIs.
namespace Foundation
{
    internal enum NSStringEncoding { UTF8 }

    internal sealed class NSData
    {
        private readonly string _value;

        private NSData(string value) => _value = value;
        internal static NSData FromString(string value, NSStringEncoding encoding) => new NSData(value);
        internal string ToString(NSStringEncoding encoding) => _value;
    }
}

namespace Security
{
    internal enum SecKind { GenericPassword }
    internal enum SecAccessible { Always, AfterFirstUnlockThisDeviceOnly }
    internal enum SecStatusCode { Success = 0, DuplicateItem = -25299, ItemNotFound = -25300, MissingEntitlement = -34018 }

    internal sealed class SecRecord
    {
        internal SecRecord() { }
        internal SecRecord(SecKind kind) { }
        internal string Account { get; set; }
        internal string Service { get; set; }
        internal string Generic { get; set; }
        internal int CreatorType { get; set; }
        internal string AccessGroup { get; set; }
        internal Foundation.NSData ValueData { get; set; }
        internal SecAccessible Accessible { get; set; }
        internal bool Synchronizable { get; set; }

        internal SecRecord Copy() => (SecRecord)MemberwiseClone();
    }

    internal static class SecKeyChain
    {
        private static readonly object s_lock = new object();
        private static readonly List<SecRecord> s_records = new List<SecRecord>();
        internal static Action<SecRecord> AfterQuery { get; set; }

        internal static void Reset()
        {
            lock (s_lock)
            {
                s_records.Clear();
                AfterQuery = null;
            }
        }

        internal static SecRecord QueryAsRecord(SecRecord query, out SecStatusCode status)
        {
            var records = QueryAsRecord(query, 1, out status);
            return records.FirstOrDefault();
        }

        internal static SecRecord[] QueryAsRecord(SecRecord query, int limit, out SecStatusCode status)
        {
            SecRecord[] matches;
            lock (s_lock)
            {
                matches = s_records.Where(record => Matches(record, query))
                    .Take(limit).Select(record => record.Copy()).ToArray();
                status = matches.Length == 0 ? SecStatusCode.ItemNotFound : SecStatusCode.Success;
            }
            AfterQuery?.Invoke(query);
            return matches;
        }

        internal static SecStatusCode Add(SecRecord record)
        {
            lock (s_lock)
            {
                var stored = record.Copy();
                stored.AccessGroup ??= "TEST.com.microsoft.adalcache";
                if (s_records.Any(existing => Matches(existing, stored)))
                {
                    return SecStatusCode.DuplicateItem;
                }
                s_records.Add(stored);
                return SecStatusCode.Success;
            }
        }

        internal static SecStatusCode Update(SecRecord query, SecRecord attributes)
        {
            lock (s_lock)
            {
                var record = s_records.SingleOrDefault(existing => Matches(existing, query));
                if (record is null)
                {
                    return SecStatusCode.ItemNotFound;
                }
                record.ValueData = attributes.ValueData;
                return SecStatusCode.Success;
            }
        }

        internal static SecStatusCode Remove(SecRecord query)
        {
            lock (s_lock)
            {
                return s_records.RemoveAll(record => Matches(record, query)) == 0
                    ? SecStatusCode.ItemNotFound : SecStatusCode.Success;
            }
        }

        private static bool Matches(SecRecord record, SecRecord query) =>
            (query.Account is null || string.Equals(record.Account, query.Account, StringComparison.Ordinal)) &&
            (query.Service is null || string.Equals(record.Service, query.Service, StringComparison.Ordinal)) &&
            (query.AccessGroup is null || string.Equals(record.AccessGroup, query.AccessGroup, StringComparison.Ordinal)) &&
            (query.CreatorType == 0 || record.CreatorType == query.CreatorType);
    }
}

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal static class MsalError
    {
        internal const string CannotAccessPublisherKeyChain = "cannot_access_publisher_keychain";
        internal const string MissingEntitlements = "missing_entitlements";
    }
}
