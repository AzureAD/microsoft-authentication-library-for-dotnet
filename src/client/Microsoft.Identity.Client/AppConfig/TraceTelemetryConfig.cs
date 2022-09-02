// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using JObject = System.Text.Json.Nodes.JsonObject;
#else
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// A simple <see cref="ITelemetryConfig"/> implementation that writes data using System.Diagnostics.Trace.
    /// </summary>
    /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
    [Obsolete("Telemetry is sent automatically by MSAL.NET. See https://aka.ms/msal-net-telemetry.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TraceTelemetryConfig : ITelemetryConfig
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        public TraceTelemetryConfig()
        {
            SessionId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        public TelemetryAudienceType AudienceType => TelemetryAudienceType.PreProduction;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        public string SessionId { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        public Action<ITelemetryEventPayload> DispatchAction => payload =>
        {
            var jsonObject = new JObject();
            foreach (var kvp in payload.BoolValues)
            {
                jsonObject[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in payload.IntValues)
            {
                jsonObject[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in payload.Int64Values)
            {
                jsonObject[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in payload.StringValues)
            {
                jsonObject[kvp.Key] = kvp.Value;
            }

            string message = JsonHelper.JsonObjectToString(jsonObject);
#if WINDOWS_APP
            Debug.WriteLine(message);
#else
            Trace.TraceInformation(message);
            Trace.Flush();
#endif
        };

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        public IEnumerable<string> AllowedScopes => CollectionHelpers.GetEmptyReadOnlyList<string>();
    }
}
