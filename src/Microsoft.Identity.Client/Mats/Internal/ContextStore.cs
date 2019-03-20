// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Mats.Internal.Constants;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class ContextStore
    {
        public static ContextStore CreateContextStore(
            MatsAudienceType audienceType,
            string appName,
            string appVersion,
            string dptiInternal,
            string deviceNetworkState,
            string sessionId,
            int platform)
        {
            if (!Guid.TryParse(sessionId, out Guid result))
            {
                sessionId = MatsId.Create();
            }
            return new ContextStore(audienceType, appName, appVersion, dptiInternal, deviceNetworkState, sessionId, platform);
        }

        private ContextStore(
            MatsAudienceType audienceType,
            string appName,
            string appVersion,
            string dptiInternal,
            string deviceNetworkState,
            string sessionId,
            int platform)
        {
            AudienceType = audienceType;
            AppName = appName;
            AppVersion = appVersion;
            DptiInternal = dptiInternal;
            DeviceNetworkState = deviceNetworkState;
            SessionId = sessionId;
            Platform = platform;
        }

        public MatsAudienceType AudienceType { get; }
        public string AppName { get; }
        public string AppVersion { get; }
        public string DptiInternal { get; }
        public string DeviceNetworkState { get; }
        public string SessionId { get; }
        public int Platform { get; }

        public void AddContext(IEnumerable<IPropertyBag> propertyBags)
        {
            foreach (var propertyBag in propertyBags)
            {
                propertyBag.Add(ContextPropertyNames.AppAudienceConstStrKey, MatsConverter.AsString(AudienceType));
                propertyBag.Add(ContextPropertyNames.AppNameConstStrKey, AppName);
                propertyBag.Add(ContextPropertyNames.AppVerConstStrKey, AppVersion);
                propertyBag.Add(ContextPropertyNames.DeviceNetworkStateConstStrKey, DeviceNetworkState);
                propertyBag.Add(ContextPropertyNames.DptiConstStrKey, DptiInternal);
                propertyBag.Add(ContextPropertyNames.SessionIdConstStrKey, SessionId);
                propertyBag.Add(ContextPropertyNames.TypeConstStrKey, ContextPropertyValues.AuthenticationConstStrValue);
                propertyBag.Add(ContextPropertyNames.MatsSdkVerConstStrKey, ContextPropertyValues.MatsSdkVerConstStrValue);
                propertyBag.Add(ContextPropertyNames.PlatformConstStrKey, Platform);
            }
        }
    }
}
