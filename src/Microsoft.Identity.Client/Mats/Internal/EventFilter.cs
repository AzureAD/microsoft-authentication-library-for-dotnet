// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class EventFilter : IEventFilter
    {
        private readonly bool _enableAggregation;
        public EventFilter(bool enableAggregation)
        {
            _enableAggregation = enableAggregation;
        }

        public bool IsSilentAction(PropertyBagContents contents) => throw new NotImplementedException();
        public void SetShouldAggregate(bool shouldAggregate) => throw new NotImplementedException();
        public bool ShouldAggregateAction(PropertyBagContents contents) => throw new NotImplementedException();
    }
}
