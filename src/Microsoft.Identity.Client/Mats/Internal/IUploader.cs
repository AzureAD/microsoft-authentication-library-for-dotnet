// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal interface IUploader
    {
        string AppName {get;}
        void Upload(IEnumerable<PropertyBagContents> uploadEvents);
    }
}
