// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Test.Common
{
    public class AuthorityWithExpectedTenantId
    {
        public Uri Authority { get; set; }
        public string ExpectedTenantId { get; set; }

        public object[] ToObjectArray()
        {
            return new object[] { Authority, ExpectedTenantId };
        }
    }
}
