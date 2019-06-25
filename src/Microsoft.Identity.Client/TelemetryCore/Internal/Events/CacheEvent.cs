// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.TelemetryCore.Internal.Events
{
    internal class CacheEvent : EventBase
    {
        public const string TokenCacheLookup = EventNamePrefix + "token_cache_lookup";
        public const string TokenCacheWrite = EventNamePrefix + "token_cache_write";
        public const string TokenCacheBeforeAccess = EventNamePrefix + "token_cache_before_access";
        public const string TokenCacheAfterAccess = EventNamePrefix + "token_cache_after_access";
        public const string TokenCacheBeforeWrite = EventNamePrefix + "token_cache_before_write";
        public const string TokenCacheDelete = EventNamePrefix + "token_cache_delete";

        public const string TokenTypeKey = EventNamePrefix + "token_type";

        public CacheEvent(string eventName, string telemetryCorrelationId) : base(eventName, telemetryCorrelationId)
        {
        }

        public enum TokenTypes
        {
            AT,
            RT,
            ID,
            Account, 
            AppMetadata
        };

        public TokenTypes TokenType
        {
            set
            {
                var types = new Dictionary<TokenTypes, string>()
                {
                    {TokenTypes.AT, "at"},
                    {TokenTypes.RT, "rt"},
                    {TokenTypes.ID, "id"},
                    {TokenTypes.Account, "account"},
                    {TokenTypes.AppMetadata, "appmetadata"}
                };
                this[TokenTypeKey] = types[value];
            }
        }
    }
}
