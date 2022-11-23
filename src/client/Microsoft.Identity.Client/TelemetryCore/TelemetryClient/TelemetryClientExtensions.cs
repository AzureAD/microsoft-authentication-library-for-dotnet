// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.TelemetryCore.TelemetryClient
{
    /// <summary>
    /// Extension methods used for telemetry client.
    /// </summary>
    internal static class TelemetryClientExtensions
    {
        /// <summary>
        /// Checks if any of the clients in the set of <see cref="ITelemetryClient"/>s are enabled for a given <paramref name="eventName"/>.
        /// </summary>
        /// <param name="clients">Set of clients to check.</param>
        /// <param name="eventName">Event name to evaluate.</param>
        /// <returns>True if any of the clients are enabled for the eventName, otherwise false.</returns>
        internal static bool HasEnabledClients(this ITelemetryClient[] clients, string eventName)
        {
            foreach (var client in clients)
            {
                if (client.IsEnabled(eventName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sends the same input events to each telemetry client.
        /// </summary>
        /// <param name="clients">Clients to emit telemetry to.</param>
        /// <param name="eventDetails">Telemetry details to emit.</param>
        internal static void TrackEvent(this ITelemetryClient[] clients, TelemetryEventDetails eventDetails)
        {
            foreach (var client in clients)
            {
                if (client.IsEnabled(eventDetails.Name))
                    client.TrackEvent(eventDetails);
            }
        }
    }
}
