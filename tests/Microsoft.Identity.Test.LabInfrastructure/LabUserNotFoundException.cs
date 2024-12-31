// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabUserNotFoundException : Exception
    {
       
        public LabUserNotFoundException(string message)
            : base(message)
        {
        }

        public LabUserNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
