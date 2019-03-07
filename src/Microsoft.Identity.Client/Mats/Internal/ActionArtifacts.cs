// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class ActionArtifacts<T>
    {
        public ActionArtifacts(T action, ActionPropertyBag propertyBag)
        {
            Action = action;
            PropertyBag = propertyBag;
        }

        public T Action { get; }
        public ActionPropertyBag PropertyBag { get; }
    }
}
