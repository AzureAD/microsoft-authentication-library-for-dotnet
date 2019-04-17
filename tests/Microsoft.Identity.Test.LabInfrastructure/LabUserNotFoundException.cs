// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabUserNotFoundException : Exception
    {
        public UserQuery Parameters { get; set; }

        public LabUserNotFoundException(UserQuery parameters, string message):base(message)
        {
            Parameters = parameters;
        }
    }
}
