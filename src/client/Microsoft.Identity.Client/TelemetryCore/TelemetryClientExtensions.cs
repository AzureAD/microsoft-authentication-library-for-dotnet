// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.TelemetryCore
{
    /// <summary>
    /// Extension methods used for telemetry client.
    /// </summary>
    internal static class TelemetryClientExtensions
    {
        /// <summary>
        /// Gets a set of <see cref="ITelemetryClient"/>s which are enabled for a given <paramref name="eventName"/>.
        /// </summary>
        /// <param name="clients">Set of clients to check.</param>
        /// <param name="eventName">Event name to evaluate.</param>
        /// <returns>The relevant set of <see cref="ITelemetryClient"/>s.</returns>
        internal static IEnumerable<ITelemetryClient> GetEnabledClients(this IEnumerable<ITelemetryClient> clients, string eventName)
        {
            return clients?.Where(c => c.IsEnabled(eventName));
        }

        /// <summary>
        /// Sends the same input events to each telemetry client.
        /// </summary>
        /// <param name="clients">Clients to emit telemetry to.</param>
        /// <param name="eventDetails">Telemetry details to emit.</param>
        internal static void TrackEvent(this IEnumerable<ITelemetryClient> clients, TelemetryEventDetails eventDetails, string eventName)
        {
            foreach (var client in clients.GetEnabledClients(eventName))
            {
                client.TrackEvent(eventDetails);
            }
        }
    }
}
