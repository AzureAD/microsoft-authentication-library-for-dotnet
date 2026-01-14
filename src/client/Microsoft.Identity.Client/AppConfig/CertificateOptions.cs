// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Represents configuration options for certificate handling or management.
    /// </summary>
    public class CertificateOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the X.509 certificate chain (x5c) should be included in the token
        /// request.
        /// </summary>
        /// <remarks>Set this property to <see langword="true"/> to include X5C in the token request; 
        /// otherwise, set it to <see langword="false"/>.</remarks>
        public bool SendX5C { get; set; } = false;
    }
}
