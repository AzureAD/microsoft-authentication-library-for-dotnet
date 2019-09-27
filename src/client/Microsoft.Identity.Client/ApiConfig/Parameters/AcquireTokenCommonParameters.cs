// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenCommonParameters
    {
        private readonly Dictionary<string, string> _apiTelemetry = new Dictionary<string, string>();
        public ApiEvent.ApiIds ApiId { get; set; } = ApiEvent.ApiIds.None;
        public Guid CorrelationId { get; set; }
        public Guid UserProvidedCorrelationId { get; set; }
        public bool UseCorrelationIdFromUser { get; set; }
        public IEnumerable<string> Scopes { get; set; }
        public IDictionary<string, string> ExtraQueryParameters { get; set; }
        public string Claims { get; set; }
        public AuthorityInfo AuthorityOverride { get; set; }
        public ApiTelemetryId ApiTelemId { get; set; } = ApiTelemetryId.Unknown;

        public IAuthenticationScheme AuthenticationScheme { get; set; } = new BearerAuthenticationScheme();

        public void AddApiTelemetryFeature(ApiTelemetryFeature feature)
        {
            _apiTelemetry[MatsConverter.AsString(feature)] = TelemetryConstants.True;
        }

        public void AddApiTelemetryFeature(ApiTelemetryFeature feature, bool isTrue)
        {
            string telemetryEnabled = TelemetryConstants.False;
            if (isTrue)
            {
                telemetryEnabled = TelemetryConstants.True;
            }

            _apiTelemetry[MatsConverter.AsString(feature)] = telemetryEnabled;
        }

        public IEnumerable<KeyValuePair<string, string>> GetApiTelemetryFeatures()
        {
            return _apiTelemetry;
        }
    }
}
