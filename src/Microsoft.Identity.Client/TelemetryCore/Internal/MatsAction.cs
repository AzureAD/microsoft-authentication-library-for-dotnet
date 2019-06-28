// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class MatsAction
    {
        public MatsAction(string actionId, MatsScenario scenario, string correlationId)
        {
            ActionId = actionId;
            Scenario = scenario;
            CorrelationId = correlationId;
        }

        public string ActionId { get; }
        public MatsScenario Scenario { get; }
        public string CorrelationId { get; }
    }
}
