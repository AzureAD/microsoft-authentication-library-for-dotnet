// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class OsCapabilitiesExtensionsConfidential // TODO MSAL5: move these to their parent types
    {
        /// <summary>
        /// Returns the certificate used to create this <see cref="ConfidentialClientApplication"/>, if any.
        /// </summary>
        public static X509Certificate2 GetCertificate(this IConfidentialClientApplication confidentialClientApplication)
        {

            if (confidentialClientApplication is ConfidentialClientApplication cca)
            {
                return cca.Certificate;
            }

            throw new ArgumentException("This extension method is only available for the ConfidentialClientApplication implementation " +
                "of the IConfidentialClientApplication interface.");
        }
    }
}
