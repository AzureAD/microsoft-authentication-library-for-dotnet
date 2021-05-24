// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Kerberos
{
    /// <summary>
    /// The Kerberos key types used in this assembly.
    /// </summary>
    public enum KerberosKeyTypes
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// dec-cbc-crc ([RFC3961] section 6.2.3)
        /// </summary>
        DecCbcCrc = 1,

        /// <summary>
        /// des-cbc-md5 ([RFC3961] section 6.2.1)
        /// </summary>
        DesCbcMd5 = 3,

        /// <summary>
        /// aes128-cts-hmac-sha1-96 ([RFC3962] section 6)
        /// </summary>
        Aes128CtsHmacSha196 = 17,

        /// <summary>
        /// aes256-cts-hmac-sha1-96 ([RFC3962] section 6)
        /// </summary>
        Aes256CtsHmacSha196 = 18,
    }
}
