// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Identity.Client.Mats.Internal.Constants;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal static class ActionComparer
    {
        public static bool IsEquivalentClass(ActionPropertyBag action1, ActionPropertyBag action2)
        {
            if (action1 == action2 || action1.ReadyForUpload || action2.ReadyForUpload || !action1.IsAggregable || !action2.IsAggregable)
            {
                return false;
            }

            var contents1 = action1.GetContents();
            var contents2 = action2.GetContents();
            var v = GetComparisonStringProperties();

            foreach (string s in v)
            {
                if (!IsPropertyEquivalent(s, contents1.StringProperties, contents2.StringProperties))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPropertyEquivalent(string propertyName, ConcurrentDictionary<string, string> propertyMap1, ConcurrentDictionary<string, string> propertyMap2)
        {
            string value1 = string.Empty;
            string value2 = string.Empty;
            bool containsProperty1 = propertyMap1.TryGetValue(propertyName, out value1);
            bool containsProperty2 = propertyMap2.TryGetValue(propertyName, out value2);
        
            return containsProperty1 == containsProperty2 && string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static List<string> GetComparisonStringProperties()
        {
            return new List<string>
            {
                ActionPropertyNames.AccountIdConstStrKey,
                ActionPropertyNames.ActionTypeConstStrKey,
                MsalTelemetryBlobEventNames.BrokerAppConstStrKey,
                MsalTelemetryBlobEventNames.TenantIdConstStrKey,
                ActionPropertyNames.IdentityConstStrKey,
                ActionPropertyNames.IdentityServiceConstStrKey,
                MsalTelemetryBlobEventNames.IdpConstStrKey,
                ActionPropertyNames.TenantIdConstStrKey,
                ActionPropertyNames.ResourceConstStrKey,
                ActionPropertyNames.ScopeConstStrKey
            };
        }
    }
}
