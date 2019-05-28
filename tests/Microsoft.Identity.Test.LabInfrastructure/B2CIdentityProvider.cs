// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public enum B2CIdentityProvider
    {
        None = 0, // Non-B2C user
        Local = 1, // Local B2C account
        Facebook = 2,
        Google = 3,
        MSA = 4
    }
}
