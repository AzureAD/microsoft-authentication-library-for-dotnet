// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class ActionArtifacts
    {
        public ActionArtifacts(MatsAction action, ActionPropertyBag propertyBag)
        {
            Action = action;
            PropertyBag = propertyBag;
        }

        public MatsAction Action { get; }
        public ActionPropertyBag PropertyBag { get; }
    }
}
