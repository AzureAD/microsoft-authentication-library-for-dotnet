// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal interface IEventFilter
    {
        bool ShouldAggregateAction(PropertyBagContents contents);
        bool IsSilentAction(PropertyBagContents contents);
        void SetShouldAggregate(bool shouldAggregate);
    }
}
