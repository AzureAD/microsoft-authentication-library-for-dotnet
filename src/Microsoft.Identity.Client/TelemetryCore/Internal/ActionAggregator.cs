// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal static class ActionAggregator
    {
        public static void AggregateActions(ActionPropertyBag targetAction, ActionPropertyBag childAction)
        {
            targetAction.IncrementCount();
            targetAction.Sum(ActionPropertyNames.CountConstStrKey, 1);

            var childContents = childAction.GetContents();

            foreach (string s in GetIntAggregationProperties())
            {
                AggregateMax(s, targetAction, childContents.IntProperties);
                AggregateMin(s, targetAction, childContents.IntProperties);
                AggregateSum(s, targetAction, childContents.IntProperties);
            }

            foreach (string s in GetInt64AggregationProperties())
            {
                AggregateMax(s, targetAction, childContents.Int64Properties);
                AggregateMin(s, targetAction, childContents.Int64Properties);
                AggregateSum(s, targetAction, childContents.Int64Properties);
            }
        }

        private static void AggregateMax(string basePropertyName, ActionPropertyBag targetAction, ConcurrentDictionary<string, int> childMap)
        {
            string fullPropertyname = basePropertyName + ActionPropertyNames.MaxConstStrSuffix;

            if (ShouldAggregateProperty<int>(fullPropertyname, childMap, out int childValue))
            {
                targetAction.Max(fullPropertyname, childValue);
            }
        }

        private static void AggregateMin(string basePropertyName, ActionPropertyBag targetAction, ConcurrentDictionary<string, int> childMap)
        {
            string fullPropertyname = basePropertyName + ActionPropertyNames.MinConstStrSuffix;

            if (ShouldAggregateProperty<int>(fullPropertyname, childMap, out int childValue))
            {
                targetAction.Min(fullPropertyname, childValue);
            }
        }

        private static void AggregateSum(string basePropertyName, ActionPropertyBag targetAction, ConcurrentDictionary<string, int> childMap)
        {
            string fullPropertyname = basePropertyName + ActionPropertyNames.SumConstStrSuffix;

            if (ShouldAggregateProperty<int>(fullPropertyname, childMap, out int childValue))
            {
                targetAction.Sum(fullPropertyname, childValue);
            }
        }

        private static void AggregateMax(string basePropertyName, ActionPropertyBag targetAction, ConcurrentDictionary<string, long> childMap)
        {
            string fullPropertyname = basePropertyName + ActionPropertyNames.MaxConstStrSuffix;

            if (ShouldAggregateProperty<long>(fullPropertyname, childMap, out long childValue))
            {
                targetAction.Max(fullPropertyname, childValue);
            }
        }

        private static void AggregateMin(string basePropertyName, ActionPropertyBag targetAction, ConcurrentDictionary<string, long> childMap)
        {
            string fullPropertyname = basePropertyName + ActionPropertyNames.MinConstStrSuffix;

            if (ShouldAggregateProperty<long>(fullPropertyname, childMap, out long childValue))
            {
                targetAction.Min(fullPropertyname, childValue);
            }
        }

        private static void AggregateSum(string basePropertyName, ActionPropertyBag targetAction, ConcurrentDictionary<string, long> childMap)
        {
            string fullPropertyname = basePropertyName + ActionPropertyNames.SumConstStrSuffix;

            if (ShouldAggregateProperty<long>(fullPropertyname, childMap, out long childValue))
            {
                targetAction.Sum(fullPropertyname, childValue);
            }
        }

        private static bool ShouldAggregateProperty<T>(string propertyName, ConcurrentDictionary<string, T> childMap, out T childValue)
        {
            return childMap.TryGetValue(propertyName, out childValue);
        }

        private static List<string> GetIntAggregationProperties()
        {
            return new List<string>
            {
                MsalTelemetryBlobEventNames.CacheEventCountConstStrKey, 
                MsalTelemetryBlobEventNames.HttpEventCountTelemetryBatchKey, 
                MsalTelemetryBlobEventNames.ResponseTimeConstStrKey 
            };
        }

        private static List<string> GetInt64AggregationProperties()
        {
            return new List<string>
            {
                ActionPropertyNames.DurationConstStrKey, 
            };
        }
    }
}
