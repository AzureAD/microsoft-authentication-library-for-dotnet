// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class MatsAction
    {
        public MatsAction(string actionId, MatsScenario scenario)
        {
            ActionId = actionId;
            Scenario = scenario;
        }

        public string ActionId {get;}
        public MatsScenario Scenario {get;}
    }
}
