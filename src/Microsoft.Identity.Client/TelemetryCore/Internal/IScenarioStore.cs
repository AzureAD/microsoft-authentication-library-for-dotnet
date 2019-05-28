// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal interface IScenarioStore
    {
        MatsScenario CreateScenario();
        IEnumerable<IPropertyBag> GetEventsForUpload();
        void ClearCompletedScenarios();
        void NotifyActionCompleted(string scenarioId);
    }
}
