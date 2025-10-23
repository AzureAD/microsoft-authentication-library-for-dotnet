// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal interface ICertificateRepository
    {
        void TryInstallWithFriendlyName(X509Certificate2 cert, string friendlyName);

        bool TryResolveFreshestBySubjectAndType(
            string subjectCn, string subjectDc, string tokenType,
            out X509Certificate2 cert, out string mtlsEndpoint);

        // Cleanup helpers
        void PurgeExpiredBeyondWindow(string subjectCn, string subjectDc, TimeSpan grace);
        void RemoveAllWithFriendlyNamePrefixForTest(string friendlyNamePrefix);
    }
}
