// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class Scenario : IScenarioHandle
    {
        public Scenario(string scenarioId, int actionCount)
        {
            ScenarioId = scenarioId;
            ActionCount = actionCount;
        }

        public string ScenarioId {get;}
        public int ActionCount {get;}
    }
}
