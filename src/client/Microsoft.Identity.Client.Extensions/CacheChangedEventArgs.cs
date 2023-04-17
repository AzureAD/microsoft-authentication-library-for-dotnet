// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// Event args describing which accounts have been added or removed on a cache change
    /// </summary>
    public class CacheChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets an enumerable of <see cref="AccountId.Identifier"/> for each account added to the cache.
        /// </summary>
        public readonly IEnumerable<string> AccountsAdded;

        /// <summary>
        /// Gets an enumerable of <see cref="AccountId.Identifier"/> for each account removed from the cache.
        /// </summary>
        public readonly IEnumerable<string> AccountsRemoved;

        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="added">An enumerable of <see cref="AccountId.Identifier"/> for each account added to the cache.</param>
        /// <param name="removed">An enumerable of <see cref="AccountId.Identifier"/> for each account removed from the cache.</param>
        public CacheChangedEventArgs(IEnumerable<string> added, IEnumerable<string> removed)
        {
            AccountsAdded = added;
            AccountsRemoved = removed;
        }
    }
}
