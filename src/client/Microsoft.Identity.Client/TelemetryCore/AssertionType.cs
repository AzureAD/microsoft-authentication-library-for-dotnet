// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal enum AssertionType
    {
        None = 0,
        CertificateWithoutSni = 1,
        CertificateWithSni = 2,
        Secret = 3,
        ClientAssertion = 4,
        ManagedIdentity = 5
    }
}
