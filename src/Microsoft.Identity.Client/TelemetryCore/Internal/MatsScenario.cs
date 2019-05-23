// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class MatsScenario
    {
        public MatsScenario(string scenarioId, int actionCount)
        {
            ScenarioId = scenarioId;
            ActionCount = actionCount;
        }

        public string ScenarioId {get;}
        public int ActionCount {get;}
    }
}
