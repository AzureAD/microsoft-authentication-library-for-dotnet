// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// A simple <see cref="ITelemetryConfig"/> implementation that writes data using <see cref="Trace"/>
    /// </summary>
    /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
    public class TraceTelemetryConfig : ITelemetryConfig
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        public TraceTelemetryConfig()
        {
            SessionId = Guid.NewGuid().AsMatsCorrelationId();
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
            var j = new JObject();
            foreach (var kvp in payload.BoolValues)
            {
                j[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in payload.IntValues)
            {
                j[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in payload.Int64Values)
            {
                j[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in payload.StringValues)
            {
                j[kvp.Key] = kvp.Value;
            }

            string msg = JsonConvert.SerializeObject(j, Formatting.None);
#if WINDOWS_APP || NETSTANDARD1_3
            Debug.WriteLine(msg);
#else
            Trace.TraceInformation(msg);
            Trace.Flush();
#endif
        };

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>This API is experimental and it may change in future versions of the library without an major version increment</remarks>
        public IEnumerable<string> AllowedScopes => new List<string>();
    }
}
