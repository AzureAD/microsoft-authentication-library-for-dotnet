// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Describes the strength with which a token can be bound to a cryptographic key on the
    /// current host. Higher values indicate stronger binding. The value reflects what the host
    /// is capable of producing, not what a particular request used.
    /// </summary>
    /// <remarks>
    /// This type is shared by managed identity and confidential client mTLS Proof-of-Possession
    /// scenarios. A value greater than <see cref="Bearer"/> means the host can bind a token to a
    /// key; it does <b>not</b> by itself imply hardware attestation. Attestation corresponds to
    /// the <see cref="KeyGuard"/> tier specifically.
    /// </remarks>
    public enum MtlsBindingStrength
    {
        /// <summary>
        /// No key binding is available; only bearer tokens can be issued. This is the floor of
        /// the range (for example, on .NET Framework 4.6.2, which does not support PoP).
        /// </summary>
        Bearer = 0,

        /// <summary>
        /// The token can be bound to a software-backed key (for example, a persisted CNG key on
        /// Windows, or a software RSA key elsewhere). The key is not hardware-isolated.
        /// </summary>
        Software = 1,

        // 2 is reserved for a future tier (for example, TPM-backed keys).

        /// <summary>
        /// The token can be bound to a key isolated by Virtualization-based Security (VBS), such
        /// as KeyGuard on a Trusted Launch (TVM) or Confidential (CVM) virtual machine. This is
        /// the only tier that implies hardware-backed attestation.
        /// </summary>
        KeyGuard = 3
    }
}
