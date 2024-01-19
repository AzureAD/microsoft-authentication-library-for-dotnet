// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal class ThrottlingCacheEntry
    {
        public ThrottlingCacheEntry(
            MsalServiceException exception, 
            TimeSpan lifetime)
        {
            Exception = Guard.AgainstNull(exception);
            CreationTime = DateTimeOffset.UtcNow;
            ExpirationTime = CreationTime.Add(lifetime);
        }

        public ThrottlingCacheEntry(MsalServiceException exception, DateTimeOffset creationTime, DateTimeOffset expirationTime)
        {
            Exception = Guard.AgainstNull(exception);
            CreationTime = creationTime;
            ExpirationTime = expirationTime;
        }

        public MsalServiceException Exception { get; }
        public DateTimeOffset CreationTime { get; }
        public DateTimeOffset ExpirationTime { get; }

        public bool IsExpired
        {
            get
            {
                return ExpirationTime < DateTimeOffset.Now ||  // expiration in the past
                    CreationTime > DateTimeOffset.Now;      // creation in the future (i.e. user changed the machine time)
            }
        }
    }
}
