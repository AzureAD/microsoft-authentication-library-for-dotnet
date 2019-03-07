// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal interface IEventFilter
    {
        bool ShouldAggregateAction(PropertyBagContents contents);
        bool IsSilentAction(PropertyBagContents contents);
        void SetShouldAggregate(bool shouldAggregate);
    }
}
