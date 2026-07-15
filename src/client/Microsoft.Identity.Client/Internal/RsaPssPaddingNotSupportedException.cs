// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Internal
{
    internal sealed class RsaPssPaddingNotSupportedException : CryptographicException
    {
        public RsaPssPaddingNotSupportedException(CryptographicException innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}
